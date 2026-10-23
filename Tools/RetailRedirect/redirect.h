#pragma once

#include <windows.h>

// Reads iphlpapi.ini next to the DLL and starts redirecting the names it lists.
void Redirect_Start(HINSTANCE self);
