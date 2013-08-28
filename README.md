Mictlanix Business Essentials
=============================

Sales, inventory control, invoicing, accounting and more...

Instructions
------------

1. Install NuGet Command Line

        Download it from http://nuget.codeplex.com/releases

2. Make a Packages directory

        $ mkdir Packages

3. Download libraries using nuget

        $ nuget install Model/packages.config -o Packages
        $ nuget install WebApp/packages.config -o Packages

4. Make a MVC 3 directory inside Packages
 
        $ mkdir Packages/AspNetMVC.3.0.0.0

5. Copy MVC 3 dll files

        $ cp <mvc-path>/System.Web.Mvc.dll Packages/AspNetMVC.3.0.0.0/
        $ cp <mvc-path>/System.Web.Helpers.dll Packages/AspNetMVC.3.0.0.0/
        $ cp <mvc-path>/System.Web.Razor.dll Packages/AspNetMVC.3.0.0.0/
        $ cp <mvc-path>/System.Web.WebPages.dll Packages/AspNetMVC.3.0.0.0/
        $ cp <mvc-path>/System.Web.WebPages.Razor.dll Packages/AspNetMVC.3.0.0.0/
        $ cp <mvc-path>/System.Web.WebPages.Deployment.dll Packages/AspNetMVC.3.0.0.0/
