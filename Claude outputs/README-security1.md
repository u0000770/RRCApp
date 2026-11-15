# Security step 1: passwords and config

## What I found (read-only audit, 15 Nov 2026)

| # | Finding | Risk |
|---|---|---|
| 1 | The Azure SQL **username and password are in plain text** in `appsettings.json` in RRCApp, PublicApp and rrcapi. | High |
| 2 | That password is in **git history**: 2 commits in `u0000770/RRCApp`, 1 in `u0000770/PublicRuningApp`, both pushed to GitHub. Removing it from the files now does **not** remove it from history. Anyone who has seen or cloned either repo, or anyone at all if they're public, has it. | **High: the password must be changed** |
| 3 | All three apps connect with the **same SQL login**, almost certainly the server admin. A leak or bug in any app gets full control of the database, including dropping tables. | High |
| 4 | rrcapi shows **Swagger on Azure**, a browsable list of every endpoint, to anyone. | Low |
| ✓ | Encryption is on (`Encrypt=True`, `TrustServerCertificate=False`). Publish-profile passwords (`*.pubxml.user`) are not in git. Local runs use LocalDB with Windows sign-in, with no password. | OK |

## The fix

- The apps get their own **least-privilege database user** (`rrc_app`: read and write data only).
- That user's connection string is stored **in Azure** (App Service → Connection strings), not in any file.
- The `appsettings.json` files then contain **no secrets** and are safe in git.
- Finally the **admin password is changed**, so the leaked one stops working.

No code changes are needed for the connection string. .NET reads an App Service connection string named `RRCAzure` in place of the one in `appsettings.json`, and locally `appsettings.Development.json` already points at LocalDB.

## Files in this package

| File (`C:\Users\u0000770PC\source\repos\…`) | New / Replace | What |
|---|---|---|
| `RRCConfig\Sql\002_create_app_login.sql` | New | Creates database user `rrc_app` (read/write data only) |
| `RRCApp\appsettings.json` | Replace | No `ConnectionStrings`; also no `SeasonSettings` (finishes Batch 5 part C) |
| `PublicApp\appsettings.json` | Replace | Same |
| `rrcapi\appsettings.json` | Replace | No `ConnectionStrings` |
| `rrcapi\Program.cs` | Replace | Your Batch 5 version, with Swagger shown only when running locally |

## Do it in this order

**1. Create the app user** (Azure portal → SQL databases → `free-sql-db-0703531` → Query editor, signed in as the admin):
   - Open `002_create_app_login.sql`, and replace both `<<CHOOSE-A-STRONG-PASSWORD>>` placeholders with a new random password (24+ characters, from a password manager).
   - Run it. The check at the end should show `rrc_app` with `db_datareader` and `db_datawriter`.
   - **Don't save the script with the real password in it.** Undo the edit, or close it without saving.

**2. Give each App Service the new connection string.** Do this in all three: **RRCAdmin**, **rrcrrc** and **rrcapi20260120094042**.
   - Portal → App Service → **Settings → Environment variables → Connection strings → + Add**
   - Name: `RRCAzure`
   - Value (put in your new password):
     `Server=tcp:rrcserver.database.windows.net,1433;Initial Catalog=free-sql-db-0703531;Persist Security Info=False;User ID=rrc_app;Password=YOUR-NEW-PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;`
   - Type: **SQLAzure** → Apply → Confirm. The app restarts.
   - Open each site and check it works. It's now using `rrc_app`, before you've changed any code.

**3. Update the solution:**
   - Copy in the 5 files from this package.
   - Finish Batch 5: delete `RRCApp\Class.cs` and the `RRCApp\App_Data` folder.
   - Rebuild, and run locally. It uses LocalDB, so nothing changes.

**4. Publish** RRCApp, PublicApp and rrcapi.
   - **Before publishing,** make sure `001_create_config_season.sql` has been run on Azure. The season batches need it.
   - If a site fails to start with *"Connection string 'RRCAzure' not found"*, step 2 was missed for that App Service.

**5. Change the leaked admin password:**
   - Portal → SQL servers → `rrcserver` → **Overview → Reset password**.
   - Update anywhere you saved it: your SSMS connection, and the Azure connection in your Visual Studio Data Compare.
   - Only do this **after** steps 2–4, because until then the apps still use it.

**6. Commit and push** RRCApp and PublicApp. Their `appsettings.json` no longer contains a password. The old password stays in git history, but after step 5 it doesn't work.

## Optional, 5 minutes in the portal

- **Each App Service → Configuration → General settings:**
  - HTTPS Only **On**.
  - Minimum TLS **1.2**.
  - FTP state **Disabled**, if you only publish with the *Web Deploy* profiles. PublicApp also has FTP and Zip Deploy profiles.
- **SQL server → Networking:**
  - Keep "Allow Azure services and resources to access this server" (the apps need it).
  - Remove any firewall rules for IP addresses you no longer use.

## If you ever need to run locally against Azure

Don't put the connection string in `appsettings.json`. In Solution Explorer, right-click the project → **Manage User Secrets**, and put the `ConnectionStrings:RRCAzure` value there. It's stored in your Windows profile, outside the solution and git.
