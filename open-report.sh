#!/bin/bash
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
npx --yes allure@3.33.0 open "$DIR"
