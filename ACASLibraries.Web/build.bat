echo.
echo Building...
echo.
set directory=%1

cd "%directory%"
If Not Exist "C:\Program Files\nodejs\npm" (
	echo Cannot find npm in C:\Program Files\nodejs. If it is not installed, please download it from the nodejs website and install it there. The build will fail now.
)

rem Uncommenting the next line will remove all the devDependencies so that they have to be reinstalled. Can be useful if we're not cleaning before we build but we want to clean node_modules
rem call "C:\Program Files\nodejs\npm" prune --production

call "C:\Program Files\nodejs\npm" run build