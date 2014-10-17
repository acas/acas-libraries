echo.
echo Cleaning output directories and node_modules...
echo.

rmdir "%1\dist" /s /q 
rmdir "%1\bin" /s /q 
rmdir "%1\obj" /s /q 
rmdir "%1\node_modules" /s /q 
