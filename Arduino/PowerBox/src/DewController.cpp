#include "DewController.h"

#include "Debug.h"

Heater primaryHeater;
Heater secondaryHeater;
Heater mirrorFan;

void setupDewController()
{    
    //setup heaters
    primaryHeater.mosfetIsOn = false;
    primaryHeater.dutyCyclesOn = 0;
    primaryHeater.dutyCyclesOff = 0;
    primaryHeater.currentDutyCycle = 0;    
    primaryHeater.mosfetPin = 11;  
    primaryHeater.currentDutyCycleLabel = -1;
    
    secondaryHeater.mosfetIsOn = false;
    secondaryHeater.dutyCyclesOn = 0;
    secondaryHeater.dutyCyclesOff = 0;
    secondaryHeater.currentDutyCycle = 0;    
    secondaryHeater.mosfetPin = 2;  
    secondaryHeater.currentDutyCycleLabel = -1;
    
    mirrorFan.mosfetIsOn = false;
    mirrorFan.dutyCyclesOn = 0;
    mirrorFan.dutyCyclesOff = 0;
    mirrorFan.currentDutyCycle = 0;    
    mirrorFan.mosfetPin = 12;  
    mirrorFan.currentDutyCycleLabel = -1;
    
    //setup mosfets
    pinMode(primaryHeater.mosfetPin, OUTPUT);
    pinMode(secondaryHeater.mosfetPin, OUTPUT);
    pinMode(mirrorFan.mosfetPin, OUTPUT);

    //initialize state    
    digitalWrite(primaryHeater.mosfetPin, LOW);
    digitalWrite(secondaryHeater.mosfetPin, LOW);
    digitalWrite(mirrorFan.mosfetPin, LOW);
}

void updateDutyCycleCounters(Heater& heater, int dutyCycle)
{    
    if(dutyCycle >95)
    {
        if(heater.currentDutyCycleLabel != 100)
        {
            //DEBUG_SERIAL.print("Full power: ");
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //full power
            heater.dutyCyclesOn = 1;
            heater.dutyCyclesOff = 0;
            heater.currentDutyCycleLabel = 100;
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 90)
    {
        if(heater.currentDutyCycleLabel != 95)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //3/4 power
            heater.dutyCyclesOn = 19;
            heater.dutyCyclesOff = 1;
            heater.currentDutyCycleLabel = 95;
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 85) 
    {
        if(heater.currentDutyCycleLabel!=90)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //half power
            heater.dutyCyclesOn = 9;
            heater.dutyCyclesOff = 1;         
            heater.currentDutyCycleLabel = 90;   
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 80)
    {
        if(heater.currentDutyCycleLabel!=85)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 17;
            heater.dutyCyclesOff = 3;        
            heater.currentDutyCycleLabel = 85;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 75)
    {
        if(heater.currentDutyCycleLabel!=80)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 4;
            heater.dutyCyclesOff = 1;        
            heater.currentDutyCycleLabel = 80;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 70)
    {
        if(heater.currentDutyCycleLabel!=75)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 3;
            heater.dutyCyclesOff = 1;        
            heater.currentDutyCycleLabel = 75;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 65)
    {
        if(heater.currentDutyCycleLabel!=70)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 7;
            heater.dutyCyclesOff = 3;        
            heater.currentDutyCycleLabel = 70;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 60)
    {
        if(heater.currentDutyCycleLabel!=65)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 13;
            heater.dutyCyclesOff = 7;        
            heater.currentDutyCycleLabel = 65;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 55)
    {
        if(heater.currentDutyCycleLabel!=60)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 3;
            heater.dutyCyclesOff = 2;        
            heater.currentDutyCycleLabel = 60;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 50)
    {
        if(heater.currentDutyCycleLabel!=55)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 11;
            heater.dutyCyclesOff = 9;        
            heater.currentDutyCycleLabel = 55;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 45)
    {
        if(heater.currentDutyCycleLabel!=50)
        {
           // DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 1;
            heater.dutyCyclesOff = 1;        
            heater.currentDutyCycleLabel = 50;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 40)
    {
        if(heater.currentDutyCycleLabel!=45)
        {
           // DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 9;
            heater.dutyCyclesOff = 11;        
            heater.currentDutyCycleLabel = 45;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 35)
    {
        if(heater.currentDutyCycleLabel!=40)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 2;
            heater.dutyCyclesOff = 3;        
            heater.currentDutyCycleLabel = 40;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 30)
    {
        if(heater.currentDutyCycleLabel!=35)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 7;
            heater.dutyCyclesOff = 13;        
            heater.currentDutyCycleLabel = 35;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 25)
    {
        if(heater.currentDutyCycleLabel!=30)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 3;
            heater.dutyCyclesOff = 7;        
            heater.currentDutyCycleLabel = 30;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 20)
    {
        if(heater.currentDutyCycleLabel!=25)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 1;
            heater.dutyCyclesOff = 3;        
            heater.currentDutyCycleLabel = 25;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 15)
    {
        if(heater.currentDutyCycleLabel!=20)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 1;
            heater.dutyCyclesOff = 4;        
            heater.currentDutyCycleLabel = 20;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 10)
    {
        if(heater.currentDutyCycleLabel!=15)
        {
           //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 3;
            heater.dutyCyclesOff = 17;        
            heater.currentDutyCycleLabel = 15;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 5)
    {
        if(heater.currentDutyCycleLabel!=10)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 1;
            heater.dutyCyclesOff = 9;        
            heater.currentDutyCycleLabel = 10;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if(dutyCycle > 0)
    {
        if(heater.currentDutyCycleLabel!=5)
        {
            //DEBUG_SERIAL.println(heater.currentDutyCycleLabel);
            //quarter power
            heater.dutyCyclesOn = 1;
            heater.dutyCyclesOff = 19;        
            heater.currentDutyCycleLabel = 5;    
            heater.currentDutyCycle = 0; //start again.        
            heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
        }
    }
    else if (heater.currentDutyCycleLabel!=0)
    {
       // DEBUG_SERIAL.println("no power");
        //off
        heater.dutyCyclesOn = 0;
        heater.dutyCyclesOff = 1;        
        heater.currentDutyCycleLabel = 0;    
        heater.currentDutyCycle = 0; //start again.        
        heater.mosfetIsOn = false; //start with the mosfets being off and starting a new cycle
    }        
}

void triggerMosfet(Heater& heater)
{
    //we have an array of cycles to be off, then cycles to be on (we start with off)
    if(heater.dutyCyclesOn == 0)
    {           
       // DEBUG_SERIAL.println("0 on duty cycles: setting off");
        //keep off
       // DEBUG_SERIAL.print("Turning off: ");
       // DEBUG_SERIAL.println(heater.mosfetPin);
        digitalWrite(heater.mosfetPin, LOW);
        heater.mosfetIsOn = false;
    }
    else if(heater.dutyCyclesOff == 0)
    {   
       // DEBUG_SERIAL.println("0 off duty cycles: setting on");
        //keep on
       // DEBUG_SERIAL.print("Turning onA: ");
       // DEBUG_SERIAL.println(heater.mosfetPin);
        digitalWrite(heater.mosfetPin, HIGH);
        heater.mosfetIsOn = true;
    }
    else if(heater.currentDutyCycle <= heater.dutyCyclesOff)
    {     
       // DEBUG_SERIAL.println("Current off duty cycle set off");
        if(heater.mosfetIsOn)
        {
            //turn off
           // DEBUG_SERIAL.print("Turning off: ");
           // DEBUG_SERIAL.println(heater.mosfetPin);
            digitalWrite(heater.mosfetPin, LOW);
            heater.mosfetIsOn = false;
        }
    }
    else
    {        
       // DEBUG_SERIAL.println("Current on duty cycle set on");
        if(!heater.mosfetIsOn)
        {
            //turn on
           // DEBUG_SERIAL.print("Turning onB: ");
           // DEBUG_SERIAL.println(heater.mosfetPin);
            digitalWrite(heater.mosfetPin, HIGH);
            heater.mosfetIsOn = true;
        }
    }

    //increment
    heater.currentDutyCycle++;
    if(heater.currentDutyCycle > (heater.dutyCyclesOff+heater.dutyCyclesOn))
    {
        heater.currentDutyCycle = 0;        
    }
}

void dealWithDewController(PublishableState publishableState)
{    
    if(publishableState.primaryHeaterConnected)
    {
        // compute duty cycles, update struct
        updateDutyCycleCounters(primaryHeater, publishableState.primaryHeaterDutyCycle);
        
        // kick mosfets if needed
        //can put in a "do this every 10 sec?"
        triggerMosfet(primaryHeater);
    }
    else
    {
        digitalWrite(primaryHeater.mosfetPin, LOW);
    }

    if(publishableState.secondaryHeaterConnected)
    {
        // compute duty cycles, update struct
        updateDutyCycleCounters(secondaryHeater, publishableState.secondaryHeaterDutyCycle);

        // kick mosfets if needed
        //can put in a "do this every 10 sec?"
        triggerMosfet(secondaryHeater);
    }
    else
    {
        digitalWrite(secondaryHeater.mosfetPin, LOW);
    }

    if(publishableState.mirrorFanConnected)
    {
        // compute duty cycles, update struct
        updateDutyCycleCounters(mirrorFan, publishableState.mirrorFanDutyCycle);

        // kick mosfets if needed
        //can put in a "do this every 10 sec?"
        triggerMosfet(mirrorFan);
    }
    else
    {
        digitalWrite(mirrorFan.mosfetPin, LOW);
    }
}

