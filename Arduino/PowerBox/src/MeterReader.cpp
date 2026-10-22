#include "MeterReader.h"
#include <Arduino.h>
#include "Debug.h"
#include <dht.h>


#define TEMP_CHECK_INTERVAL_MS 4000 //4 sec
unsigned long timestampOfNextTempCheck; // when to check the temp probes

#define ONE_WIRE_BUS 6
#include <OneWire.h>
#include <DallasTemperature.h>
OneWire oneWire(ONE_WIRE_BUS);
DallasTemperature sensors(&oneWire);
uint8_t mirrorChipSensorC11[8] = { 0x28, 0x1B, 0xBB, 0xA7, 0x00, 0x00, 0x00, 0xAD };
uint8_t mirrorChipSensorC8[8] = { 0x28, 0x0B, 0xF2, 0xA6, 0x00, 0x00, 0x00, 0x63 };
uint8_t TempProbeC8[8] = { 0x28, 0xFF, 0x4B, 0x31, 0xAC, 0x62, 0x24, 0xD2 }; //C8
uint8_t TempProbeC11[8] = { 0x28, 0xFF, 0x57, 0x31, 0xAC, 0x62, 0xE3, 0xC6 }; //C11 used for the green connector (sensor might have white on it though?)
uint8_t TempProbeC11GuideScope[8] = { 0x28, 0xFF, 0x5C, 0x32, 0xAC, 0x62, 0xF5, 0x7E }; //C11 Guide scope
uint8_t TempProbeDSLR[8] = { 0x28, 0x92, 0x1, 0x94, 0x97, 0x2, 0x3, 0xC2 };  //DSLR


//CorrectedValue = (((INPUTValue – RawLow) * ReferenceRange) / RawRange) + ReferenceLow
float getCalibratedTempDHT11Ambient(float rawTemp)
{
  //RawRange	RefRange	RawLow	RefLow
  //7.7	      9.6918	  15.7	  12.9255
  return (((rawTemp - 15.7)*9.6918)/7.7)+12.9255;  
}
float getCalibratedMirrorChipSensorC8(float rawTemp)
{
  //RawRange	RefRange	RawLow	RefLow
  //8.75	9.6918	13.81	12.9255
  return (((rawTemp - 13.81)*9.6918)/8.78)+12.9255;    
}
float getCalibratedMirrorChipSensorC11(float rawTemp)
{
  //RawRange	RefRange	RawLow	RefLow
  //9.38	    9.6313	  13.25	  12.9255
  return (((rawTemp - 13.25)*9.6313)/9.38)+12.9255;      
}
float getCalibratedTempProbeC8(float rawTemp)
{
  //RawRange	RefRange	RawLow	RefLow
  //7.12	9.6918	15.13	12.9255
  return (((rawTemp - 15.13)*9.6918)/7.12)+12.9255;    
}
float getCalibratedTempProbeC11(float rawTemp)
{
  //RawRange	RefRange	RawLow	RefLow
  //5.53	9.6313	15.25	12.9255
  return (((rawTemp - 15.25)*9.9313)/5.53)+12.9255;      
}
float getCalibratedTempProbeDSLR(float rawTemp)
{
  //RawRange	RefRange	RawLow	RefLow
  //8.62	9.6918	14.13	12.9255
  return (((rawTemp - 14.13)*9.6918)/8.62)+12.9255;    
}
float getCalibratedTempProbeC11GuideScope(float rawTemp)
{
  //RawRange	RefRange	RawLow	RefLow
  //7.13	9.6313	15	12.9255
  return (((rawTemp - 15)*9.6313)/7.13)+12.9255;        
}





#define DHT_PIN 13
DHT dht(DHT_PIN, DHT11);


void discoverOneWireDevices() {
  byte i;
  byte present = 0;
  byte data[12];
  byte addr[8];
  
  DEBUG_SERIAL.print("Looking for 1-Wire devices...\n\r");
  while(oneWire.search(addr)) {
    DEBUG_SERIAL.print("\n\rFound \'1-Wire\' device with address:\n\r");

    for( i = 0; i < 8; i++) {
      DEBUG_SERIAL.print("0x");
      if (addr[i] < 16) {
        DEBUG_SERIAL.print('0');
      }
      DEBUG_SERIAL.print(addr[i], HEX);

      if (i < 7) {
        DEBUG_SERIAL.print(", ");

      }
    }
    if ( OneWire::crc8( addr, 7) != addr[7]) {
        DEBUG_SERIAL.print("CRC is not valid!\n");
        return;
    }
  }
  DEBUG_SERIAL.print("\n\r\n\rThat's it.\r\n");
  oneWire.reset_search();
  return;
}



// reference: http://wahiduddin.net/calc/density_algorithms.htm
double computeDewPointCelcius(double celsius, double relativehumidity)
{
  double RATIO = 373.15 / (273.15 + celsius);  // RATIO was originally named A0, possibly confusing in Arduino context
  double SUM = -7.90298 * (RATIO - 1);
  SUM += 5.02808 * log10(RATIO);
  SUM += -1.3816e-7 * (pow(10, (11.344 * (1 - 1 / RATIO))) - 1);
  SUM += 8.1328e-3 * (pow(10, (-3.49149 * (RATIO - 1))) - 1);
  SUM += log10(1013.246);
  double VP = pow(10, SUM - 3) * relativehumidity;
  double T = log(VP / 0.61078);   // temp var
  return (241.88 * T) / (17.558 - T);
}

void setupMeters()
{    
    DEBUG_SERIAL.println("TODO: Setup meters");

    dht.begin();
    delay(1000);
}

void readMeters(PublishableState& publishableState)
{
    
    // read temp probes
    unsigned long currentMillis = millis();
    if (currentMillis > timestampOfNextTempCheck)
    {
        //DEBUG_SERIAL.println("Reading DHT11");

        // set the next update timestamp
        timestampOfNextTempCheck = currentMillis + TEMP_CHECK_INTERVAL_MS;
        
        // Reading temperature or humidity takes about 250 milliseconds!
        // Sensor readings may also be up to 2 seconds 'old' (its a very slow sensor)
        publishableState.ambientTempCelcius = dht.readTemperature();
        if(isnan(publishableState.ambientTempCelcius))
        {
          return;
        }
        publishableState.ambientTempCelcius = getCalibratedTempDHT11Ambient(publishableState.ambientTempCelcius);

        delay(200);
        publishableState.dewPointCelcius = computeDewPointCelcius(publishableState.ambientTempCelcius, dht.readHumidity());        
        if(isnan(publishableState.dewPointCelcius))
        {
          return;
        }
                
        // read mirror probes
        sensors.requestTemperatures();
        delay(200);

        //try chip1C11 first
        double rawTempMirror = sensors.getTempC(mirrorChipSensorC11);
        if(rawTempMirror <= DEVICE_DISCONNECTED_C)
        {
          //try chip2 C8
          rawTempMirror = sensors.getTempC(mirrorChipSensorC8);
          if(rawTempMirror <= DEVICE_DISCONNECTED_C)
          {            
            publishableState.mirrorFanConnected = false;                           
          }
          else
          {
            publishableState.mirrorFanConnected = true;                           
            publishableState.mirrorFanCelcius = getCalibratedMirrorChipSensorC8(rawTempMirror);  
          }
        }
        else
        {
          publishableState.mirrorFanConnected = true;                           
          publishableState.mirrorFanCelcius = getCalibratedMirrorChipSensorC11(rawTempMirror);
        }

        //primary heater (red or green)
        //try C8 first
        double rawTempPrimary = sensors.getTempC(TempProbeC8);
        if(rawTempPrimary <= DEVICE_DISCONNECTED_C)
        {
            //try c11
            rawTempPrimary = sensors.getTempC(TempProbeC11);
            if(rawTempPrimary <= DEVICE_DISCONNECTED_C)
            {
              publishableState.primaryHeaterConnected = false;                           
            }
            else
            {
              publishableState.primaryHeaterConnected = true;              
              publishableState.primaryHeaterCelcius = getCalibratedTempProbeC11(rawTempPrimary);
          }
        }
        else
        {
          publishableState.primaryHeaterConnected = true;              
          publishableState.primaryHeaterCelcius = getCalibratedTempProbeC8(rawTempPrimary);
        }

        //secondary heater (lcd or blue?)
        //try dslr
        double rawTempSecondary = sensors.getTempC(TempProbeDSLR);
        if(rawTempSecondary <= DEVICE_DISCONNECTED_C)
        {
          //try blue
          //try DSLR
          rawTempSecondary = sensors.getTempC(TempProbeC11GuideScope);
          if(rawTempSecondary <= DEVICE_DISCONNECTED_C)
          {
            //DEBUG_SERIAL.println("Secondary is not connected");
            publishableState.secondaryHeaterConnected = false;              
          }
          else
          {
            //DEBUG_SERIAL.println("Secondary is connected to blue?");
            publishableState.secondaryHeaterConnected = true;              
            publishableState.secondaryHeaterCelcius = getCalibratedTempProbeC11GuideScope(rawTempSecondary);
          }
        }
        else
        {
          //DEBUG_SERIAL.println("Secondary is connected to dslr?");
          publishableState.secondaryHeaterConnected = true;              
          publishableState.secondaryHeaterCelcius = getCalibratedTempProbeDSLR(rawTempSecondary); 
        }    

        //fake it:
        //publishableState.primaryHeaterCelcius = 16;
        //publishableState.primaryHeaterConnected = true;   
        //publishableState.secondaryHeaterCelcius = 20;
        //publishableState.secondaryHeaterConnected = true;   

        //calculate dutyCycle
        //1st step: percent in window:
        // negative is below dewpoint: full power!
        // 25% is close to dew point: 75% power!
        float primaryPercentInWindow = ((publishableState.primaryHeaterCelcius - publishableState.dewPointCelcius) *100.0) / (publishableState.ambientTempCelcius - publishableState.dewPointCelcius);        
        if(primaryPercentInWindow <=0)
          publishableState.primaryHeaterDutyCycle = 100;
        else if(primaryPercentInWindow >= 100)
          publishableState.primaryHeaterDutyCycle = 0;
        else
        {
          publishableState.primaryHeaterDutyCycle = 100-primaryPercentInWindow;
        }
        publishableState.primaryHeaterDutyCycle += publishableState.primaryHeaterDutyCycleOffset; //offset

        float secondaryPercentInWindow = ((publishableState.secondaryHeaterCelcius - publishableState.dewPointCelcius) *100.0) / (publishableState.ambientTempCelcius - publishableState.dewPointCelcius);        
        if(secondaryPercentInWindow <=0)
          publishableState.secondaryHeaterDutyCycle = 100;
        else if(secondaryPercentInWindow >= 100)
          publishableState.secondaryHeaterDutyCycle = 0;
        else
        {
          publishableState.secondaryHeaterDutyCycle = 100-secondaryPercentInWindow;
        }
        publishableState.secondaryHeaterDutyCycle += publishableState.secondaryHeaterDutyCycleOffset; //offset

        if(publishableState.mirrorFanMode == MIRROR_FAN_ON)
        {
          publishableState.mirrorFanDutyCycle = 100;
        }
        else if (publishableState.mirrorFanMode == MIRROR_FAN_OFF)
        {
          publishableState.mirrorFanDutyCycle = 0;
        }
        else
        {
          //0% if <= ambient                
          //100% > ambient        
          if (publishableState.mirrorFanCelcius <= (publishableState.ambientTempCelcius + publishableState.mirrorFanTempOffset )) //turn off when under 0.5C below ambient
          {
            publishableState.mirrorFanDutyCycle = 0;
          }
          else
          {
            publishableState.mirrorFanDutyCycle = 100;
          }        
          //Fans don't like being PWM, adjust temp to move threshold
      }
    }
        
    // read volt meters
    float fiveVoltRatio = 4.93;
    publishableState.fiveVolts = ((analogRead(A5) * 5.0) / 1024.0)*fiveVoltRatio; // ((read * 5V) out of bit depth) * voltage divider ratio
    float twelveVoltRatio = 5.504; 
    publishableState.twelveVolts = ((analogRead(A4) * 5.0) / 1024.0)*twelveVoltRatio; // ((read * 5V) out of bit depth) * voltage divider ratio  
    float twentyFourVoltRatio = 5.4339; 
    publishableState.twentyFourVolts = ((analogRead(A3) * 5.0) / 1024.0)*twentyFourVoltRatio; // ((read * 5V) out of bit depth) * voltage divider ratio


    // read amp meters
    //A2 = 24V line 
    //A1 = Heater1 (primary: green/red)
    //A0 = Heater2 (secondary: blue/lcd)
    publishableState.scopeAmps = (((analogRead(A2) * 5.0)/1024.0)-2.5)/0.185;
    if(publishableState.scopeAmps < 0.3)
    {
      publishableState.scopeAmps = 0;
    }
    publishableState.primaryHeaterAmps = (((analogRead(A0) * 5.0)/1024.0)-2.5)/0.185;
    if(publishableState.primaryHeaterAmps < 0.3)
    {
      publishableState.primaryHeaterAmps = 0;
    }
    publishableState.secondaryHeaterAmps = (((analogRead(A1) * 5.0)/1024.0)-2.5)/0.185;
    if(publishableState.secondaryHeaterAmps < 0.3)
    {
      publishableState.secondaryHeaterAmps = 0;
    }

   

    //update timestamp
    publishableState.lastUpdateMillis = millis();
}


