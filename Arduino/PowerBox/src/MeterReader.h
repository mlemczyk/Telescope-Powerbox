#pragma once

#include "PublishableState.h"

void setupMeters();
void readMeters(PublishableState& publishableState);

void discoverOneWireDevices();