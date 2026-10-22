#pragma once

#include "PublishableState.h"

struct Heater {    
    int dutyCyclesOn;
    int dutyCyclesOff;    
    int currentDutyCycle; //index out of (dutyCyclesOn + dutyCyclesOff)
    int currentDutyCycleLabel;

    int mosfetPin;
    bool mosfetIsOn;    
};


void setupDewController();

void dealWithDewController(PublishableState publishableState);