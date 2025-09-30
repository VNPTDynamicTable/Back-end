# aspnet-core

## How to run the project:
1. Change the connection string to the database of the project in:
   - VNPT.SNV.Migrator/appsettings.json
   - VNPT.SNV.Web.Host/appsettings.json
2. Open Package Manager Console and set the default project as VNPT.SNV.EntityFrameworkCore: Run the commands in turn to initialize the tables.
   - Add-Migration <migration-name>
   - Update-Database
3. In the Solution Explorer, right-click on VNPT.SNV.Migrator and select "Set as startup project". Then, run the project to generate the initial data.
4. Finally, in the Solution Explorer, right-click on VNPT.SNV.Web.Host and select "Set as startup project", then run the project.
