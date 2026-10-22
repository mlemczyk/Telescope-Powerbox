#include "WebServer.h"

#include "debug.h"

#include <Arduino.h>
#include <WiFiS3.h>

#include "ArduinoGraphics.h"
#include "Arduino_LED_Matrix.h"
ArduinoLEDMatrix matrix;

#include "arduino_secrets.h"  // git-ignored; copy arduino_secrets.example.h
char ssid[] = SECRET_SSID;        // your network SSID (name)
char pass[] = SECRET_PASS;        // your network password

// the IP address for the shield:
IPAddress ip(192, 168, 50, 39);    

int keyIndex = 0;                 // your network key index number (needed only for WEP)
int status = WL_IDLE_STATUS;

WiFiServer server(80);

void setupWebServer()
{
    DEBUG_SERIAL.println("Checking wifi module");
    if (WiFi.status() == WL_NO_MODULE) {
        DEBUG_SERIAL.println("Communication with WiFi module failed!");
        // don't continue
        while (true);
      }

    DEBUG_SERIAL.println("Checking wifi firmware");
    String fv = WiFi.firmwareVersion();
    if (fv < WIFI_FIRMWARE_LATEST_VERSION) {
        DEBUG_SERIAL.println("Please upgrade the firmware");
    }

    DEBUG_SERIAL.println("Setting IP address");
    //Set the IP address
    WiFi.config(ip);

    // attempt to connect to WiFi network:
    while (status != WL_CONNECTED) {
        DEBUG_SERIAL.print("Attempting to connect to SSID: ");
        DEBUG_SERIAL.println(ssid);

        // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
        status = WiFi.begin(ssid, pass);

        // wait 10 seconds for connection:
        delay(10000);
    }

    DEBUG_SERIAL.println("Starting server");
    server.begin();

    // you're connected now, so print out the status:
    DEBUG_SERIAL.print("SSID: ");
    DEBUG_SERIAL.println(WiFi.SSID());

    // print your board's IP address:
    IPAddress ip = WiFi.localIP();
    DEBUG_SERIAL.print("IP Address: ");   
    DEBUG_SERIAL.println(ip);

    // print the received signal strength:
    long rssi = WiFi.RSSI();
    DEBUG_SERIAL.print("signal strength (RSSI):");
    DEBUG_SERIAL.print(rssi);
    DEBUG_SERIAL.println(" dBm");

    //Print OK to LCD
    matrix.begin();
    matrix.beginDraw();
    matrix.stroke(0xFFFFFFFF);
    const char text[] = "OK!";
    matrix.textFont(Font_4x6);
    matrix.beginText(0, 1, 0xFFFFFF);
    matrix.println(text);
    matrix.endText();
    matrix.endDraw();
}

String urlReadString;

double avgPriDiff = 0;
long avgPriDiffCnt = 0;
double avgSecDiff = 0;
long avgSecDiffCnt = 0;
double avgMirrorDiff = 0;
long avgMirrorDiffCnt = 0;

void dealWithHTTP(PublishableState& publishableState)
{    
    // listen for incoming clients
    //handle a web server request (read request, get response from publishable state, write out response)
    WiFiClient client = server.available();
    if (client) {
        DEBUG_SERIAL.println("new client");

        // an HTTP request ends with a blank line
        boolean currentLineIsBlank = true;

        while (client.connected()) {
            if (client.available()) {
                char c = client.read();
                DEBUG_SERIAL.write(c);

                if (urlReadString.length() < 50) {
                    urlReadString += c; 
                } 

                // if you've gotten to the end of the line (received a newline
                // character) and the line is blank, the HTTP request has ended,
                // so you can send a reply
                if (c == '\n' && currentLineIsBlank) {            
                    DEBUG_SERIAL.println("got a newline and blank");

                    // send a standard HTTP response header
                    client.println("HTTP/1.1 200 OK");
                    client.println("Content-Type: text/html");
                    client.println("Connection: close");  // the connection will be closed after completion of the response                    
                    client.println();
                    client.println("<!DOCTYPE HTML>");
                    client.println("<html>");

                    // Machine-readable line for the ASCOM driver. It must stay the
                    // THIRD line of the body (after <!DOCTYPE HTML> and <html>), as
                    // exactly these 16 space-separated values in this order: the
                    // driver splits the body on '\n', takes line 2 and parses it by
                    // position (SwitchHardware..cs, updateValues()). Nothing may wrap
                    // it, so browsers show it as a row of numbers above the page.
                    char dataLine[200];
                    snprintf(dataLine, sizeof(dataLine), "%f %f %f %f %f %f %f %f %f %f %f %d %d %d %d %lu",
                        publishableState.dewPointCelcius,
                        publishableState.ambientTempCelcius,
                        publishableState.fiveVolts,
                        publishableState.twelveVolts,
                        publishableState.twentyFourVolts,
                        publishableState.scopeAmps,
                        publishableState.primaryHeaterAmps,
                        publishableState.secondaryHeaterAmps,
                        publishableState.primaryHeaterCelcius,
                        publishableState.secondaryHeaterCelcius,
                        publishableState.mirrorFanCelcius,
                        publishableState.primaryHeaterDutyCycle,
                        publishableState.secondaryHeaterDutyCycle,
                        publishableState.mirrorFanDutyCycle,
                        publishableState.mirrorFanMode,
                        publishableState.lastUpdateMillis
                        );
                    client.println(dataLine);

                    client.println("<head>");
                    client.println("<style>");
                    client.println("body {");
                    client.println("padding: 25px;");
                    client.println("background-color: black;");
                    client.println("color: white;");
                    client.println("font-size: 25px;");
                    client.println("}");  
                    client.println("</style>");
                                       

                    DEBUG_SERIAL.println("urlReadString: " + urlReadString);
                    DEBUG_SERIAL.println("\n");

                    if(urlReadString.indexOf("fanOn") > 0)
                    {                                                
                        publishableState.mirrorFanMode = MIRROR_FAN_ON;
                        DEBUG_SERIAL.println("fan: " + publishableState.mirrorFanMode);
                    }
                    else if(urlReadString.indexOf("fanOff") >0)
                    {                        
                        publishableState.mirrorFanMode = MIRROR_FAN_OFF;
                        DEBUG_SERIAL.println("fan: " + publishableState.mirrorFanMode);
                    }
                    else if(urlReadString.indexOf("fanAuto") >0)
                    {                        
                        publishableState.mirrorFanMode = MIRROR_FAN_AUTO;
                        DEBUG_SERIAL.println("fan: " + publishableState.mirrorFanMode);
                    }
                    else if(urlReadString.indexOf("priInc") >0)
                    {                        
                        publishableState.primaryHeaterDutyCycleOffset++;                        
                    }
                    else if(urlReadString.indexOf("priDec") >0)
                    {                        
                        publishableState.primaryHeaterDutyCycleOffset--;                        
                    }
                    else if(urlReadString.indexOf("secInc") >0)
                    {                        
                        publishableState.secondaryHeaterDutyCycleOffset++;                        
                    }
                    else if(urlReadString.indexOf("secDec") >0)
                    {                        
                        publishableState.secondaryHeaterDutyCycleOffset--;
                    }
                    else if(urlReadString.indexOf("fanInc") >0)
                    {                        
                        publishableState.mirrorFanTempOffset += 0.5;
                    }
                    else if(urlReadString.indexOf("fanDec") >0)
                    {                        
                        publishableState.mirrorFanTempOffset -= 0.5;
                    }
                    else if(urlReadString.indexOf("rstAvg") >0)
                    {                        
                         avgPriDiff = 0;
                         avgPriDiffCnt = 0;
                         avgSecDiff = 0;
                         avgSecDiffCnt = 0;
                         avgMirrorDiff = 0;
                         avgMirrorDiffCnt = 0;
                    }
                    else{
                        //main page is safe to refresh
                        client.println("<meta http-equiv=\"refresh\" content=\"4\">"); //refresh every 4 sec to match the probes
                    }

                    client.println("</head>");
                    client.println("<body>");

                    //write out publishableState
                    char text[400]; // make this big enough for your longest string, including a null terminator

                    //update averages
                    double primaryDiff = (publishableState.primaryHeaterCelcius-publishableState.ambientTempCelcius);
                    avgPriDiffCnt++;
                    double oldPriDiff = avgPriDiff;
                    double priCntFact = (avgPriDiffCnt - 1.0)/avgPriDiffCnt;
                    avgPriDiff = (oldPriDiff * priCntFact);
                    avgPriDiff += (primaryDiff / avgPriDiffCnt);

                    double secondaryDiff = (publishableState.secondaryHeaterCelcius-publishableState.ambientTempCelcius);
                    avgSecDiffCnt++;
                    double oldSecDiff = avgSecDiff;
                    double secCntFact = (avgSecDiffCnt - 1.0)/avgSecDiffCnt;
                    avgSecDiff = (oldSecDiff * secCntFact);
                    avgSecDiff += (secondaryDiff / avgSecDiffCnt);

                    double mirrorDiff = (publishableState.mirrorFanCelcius-publishableState.ambientTempCelcius);
                    avgMirrorDiffCnt++;
                    double oldMirrorDiff = avgMirrorDiff;
                    double mirrorCntFact = (avgMirrorDiffCnt - 1.0)/avgMirrorDiffCnt;
                    avgMirrorDiff = (oldMirrorDiff * mirrorCntFact);
                    avgMirrorDiff += (mirrorDiff / avgMirrorDiffCnt);

                    client.println("<h1>Reactor online</h1><p>");  

                    snprintf(text, sizeof(text), "DP: %f<br>Amb: %f<p>5V: %f<br>12V: %f<br>24V %f<p>PrimaryDiffC: %f<br>SecondaryDiffC: %f<br>MirrorDiffC: %f<p>PrimaryAvgDiffC: %f<br>SecondaryAvgDiffC: %f<br>MirrorAvgDiffC: %f<br>Cnt: %lu<p>PriDuty: %d<br>SecDuty: %d<br>MirrorDuty: %d<p>FanMode: %d<p>Millis: %lu",     
                        publishableState.dewPointCelcius,
                        publishableState.ambientTempCelcius,    
                        publishableState.fiveVolts,
                        publishableState.twelveVolts,                    
                        publishableState.twentyFourVolts,                        
                        primaryDiff,
                        secondaryDiff,
                        mirrorDiff,
                        avgPriDiff,
                        avgSecDiff,
                        avgMirrorDiff,
                        avgPriDiffCnt,
                        publishableState.primaryHeaterDutyCycle,
                        publishableState.secondaryHeaterDutyCycle,
                        publishableState.mirrorFanDutyCycle,
                        publishableState.mirrorFanMode,
                        publishableState.lastUpdateMillis                       
                        );
                    client.println(text);
                    client.println("<p>");  
                    client.println("<a href=\"/\">REFRESH</a>");                      
                    client.println("<p>");  
                    client.println("<a href=\"/rstAvg\">Reset Avg</a>");  
                    client.println("<p>");  
                    
                    client.println("<p>");  
                    client.println("<a href=\"/fanAuto\">Fan auto</a>");  
                    client.println("<p>");                      
                    client.println("<a href=\"/fanOff\">Fan off</a>");  
                    client.println("<p>");                      
                    client.println("<a href=\"/fanOn\">Fan on</a>");  
                    client.println("<p>");                      
                    
                    client.println("Primary Heater Duty Cycle offset: ");
                    client.println(publishableState.primaryHeaterDutyCycleOffset);
                    client.println("<br>");            
                    client.println("Increment to keep heating past ambient temp");
                    client.println("<br>");            
                    client.println("<a href=\"/priInc\">Increment</a>");  
                    client.println("<br>");            
                    client.println("<a href=\"/priDec\">Decrement</a>");  
                    client.println("<p>");            

                    client.println("Secondary Heater Duty Cycle offset: ");
                    client.println(publishableState.secondaryHeaterDutyCycleOffset);
                    client.println("<br>");            
                    client.println("Increment to keep heating past ambient temp");
                    client.println("<br>");            
                    client.println("<a href=\"/secInc\">Increment</a>");  
                    client.println("<br>");            
                    client.println("<a href=\"/secDec\">Decrement</a>");  
                    client.println("<p>");            

                    client.println("Mirror Fan temp offset: ");
                    client.println(publishableState.mirrorFanTempOffset);
                    client.println("<br>");            
                    client.println("Fan off when mirror &le; (ambient + offset)");
                    client.println("<br>");            
                    client.println("<a href=\"/fanInc\">Increment</a>");  
                    client.println("<br>");            
                    client.println("<a href=\"/fanDec\">Decrement</a>");  
                    client.println("<p>");            
                    
                    client.println("<a href=\"/\">REFRESH</a>");  

                    client.println("</body>");  
                    client.println("</html>");  
                    break;                
                }
                if (c == '\n') {
                    //DEBUG_SERIAL.println("got a newline");
                    // you're starting a new line
                    currentLineIsBlank = true;
                } else if (c != '\r') {
                    //DEBUG_SERIAL.println("got not return");
                    // you've gotten a character on the current line
                    currentLineIsBlank = false;
                }
            }
        }
    // give the web browser time to receive the data
    delay(1);

    // close the connection:
    client.stop();

    urlReadString = "";

    DEBUG_SERIAL.println("client disconnected");
  } 
}

