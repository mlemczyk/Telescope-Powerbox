#pragma once

#define MIRROR_FAN_AUTO -1
#define MIRROR_FAN_OFF 0
#define MIRROR_FAN_ON 1

// struct to store values
struct PublishableState {
  public:
    
    float dewPointCelcius;
    float ambientTempCelcius;

    float twelveVolts;
    float fiveVolts;
    float twentyFourVolts;
    
    float scopeAmps;
    float primaryHeaterAmps;
    float secondaryHeaterAmps;

    bool primaryHeaterConnected;
    bool secondaryHeaterConnected;
    bool mirrorFanConnected;

    float primaryHeaterCelcius;
    float secondaryHeaterCelcius;
    float mirrorFanCelcius;

    int primaryHeaterDutyCycle; //0-100
    int secondaryHeaterDutyCycle; //0-100
    int mirrorFanDutyCycle; //0-100
  
    int mirrorFanMode = MIRROR_FAN_AUTO;

    long lastUpdateMillis;    

    int primaryHeaterDutyCycleOffset = 10; //default = 10
    int secondaryHeaterDutyCycleOffset = 0;
    float mirrorFanTempOffset = 0;

};

