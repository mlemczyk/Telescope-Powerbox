#pragma once

#include <Arduino.h>
#include "PublishableState.h"

void setupWebServer();

void dealWithHTTP(PublishableState& publishableState);