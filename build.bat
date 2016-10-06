call "%VS140COMNTOOLS%\..\..\VC\vcvarsall.bat" x64
msbuild /verbosity:diagnostic /m /p:Configuration=Release /clp:Summary /nologo

