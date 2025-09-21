#!/bin/bash

if [ $(find /var/log/app -type f -mmin -2 | wc -l) -gt 0 ]; then
    exit 0;
else
    exit 1;
fi