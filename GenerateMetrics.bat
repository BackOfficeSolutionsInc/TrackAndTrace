

REM Create a 'GeneratedReports' folder if it does not exist
if not exist "%~dp0GeneratedReports" mkdir "%~dp0GeneratedReports"
 
REM Remove any previous test execution files to prevent issues overwriting
IF EXIST "%~dp0TractionTools.trx" del "%~dp0TractionTools.trx%"
 
REM Remove any previously created test output directories
CD %~dp0
FOR /D /R %%X IN (%USERNAME%*) DO RD /S /Q "%%X"
 
REM Run the tests against the targeted output
call :RunOpenCoverUnitTestMetrics
 
REM Generate the report output based on the test results
if %errorlevel% equ 0 (
 call :RunReportGeneratorOutput
)
 
REM Launch the report
if %errorlevel% equ 0 (
 call :RunLaunchReport
)
exit /b %errorlevel%
 REM 

:RunOpenCoverUnitTestMetrics
"%~dp0\packages\OpenCover.4.6.519\tools\OpenCover.Console.exe" ^
-register:user ^
-target:"%VS120COMNTOOLS%\..\IDE\mstest.exe" ^
-targetargs:"/testcontainer:\"%~dp0\TractionTools.Tests\bin\x86\Debug\TractionTools.Tests.dll\" /testcontainer:\"%~dp0\TractionTools.UITests\bin\x86\Debug\TractionTools.UITests.dll\" /resultsfile:\"%~dp0TractionTools.trx\"" ^
-filter:"+[RadialReview*]* -[TractionTools.Tests]* -[TractionTools.UITests]* -[*]RadialReview.RouteConfig -[*]RadialReview.RouteConfig" ^
-mergebyhash ^
-skipautoprops ^
-output:"%~dp0\GeneratedReports\TractionTools.xml"
exit /b %errorlevel%
 
:RunReportGeneratorOutput
"%~dp0\packages\ReportGenerator.2.4.5.0\tools\ReportGenerator.exe" ^
-reports:"%~dp0\GeneratedReports\TractionTools.xml" ^
-targetdir:"%~dp0\GeneratedReports\ReportGenerator Output"
exit /b %errorlevel%
 
:RunLaunchReport
start "report" "%~dp0\GeneratedReports\ReportGenerator Output\index.htm"
exit /b %errorlevel%

:RunLaunchReport
start "%~dp0TractionTools.trx"
exit /b %errorlevel%