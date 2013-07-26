LessML
======
Installation instructions:

* Install Visual Studio 2012 SDK ( http://www.microsoft.com/en-us/download/details.aspx?id=30668 )
* Run Visual Studio as Administrator (this is necessary for the build tool COM server to register)
* Build the solution

Usage instructions:
* In a project add a file with the .lx extension. Here's a sample .lx file:
	
	Window
		x:Class = MyClass
		Grid

* In that file's properties set the Custom Build Tool to LessXaml.ProjectFile
* From the file's context menu click "Run Custom Tool" (either that, or just save the file to have the tool run)
