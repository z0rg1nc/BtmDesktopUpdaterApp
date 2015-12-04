#!/bin/bash
mono ./BitMoneyUpdater.exe $@
cd ..
chmod +x ./run_on_mono.sh
./run_on_mono.sh &