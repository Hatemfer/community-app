# Connecting Grafana to your Community App

Follow these steps to set up your reports in your local Grafana instance.

## 1. Add MySQL Data Source
1. Open Grafana (`http://localhost:3000`).
2. Go to **Connections** > **Data Sources**.
3. Click **Add data source** and select **MySQL**.
4. Configure the connection:
   - **Host**: `localhost:3306` (or your MySQL port)
   - **Database**: `communityapp`
   - **User**: `root`
   - **Password**: (Empty if none)
5. Set the **UID** to `mysql-ds` (this is very important as the dashboard refers to this ID).
6. Click **Save & test**.

## Troubleshooting "No Data"
If you see "No data" in your dashboard:
1. **Time Range**: Check the time range in the top-right corner of Grafana. If it's set to "Last 6 hours" but your data was created earlier, it won't show up. Try "Last 30 days" or "Last 5 years".
2. **Database Case Sensitivity**: I have updated the queries to use `PascalCase` (e.g., `Users`). If your database uses lowercase table names, you might need to change them in the dashboard panels.
3. **Data Backfill**: I have added a migration to fix existing records that had empty `CreatedAt` dates. Make sure you have run `dotnet ef database update`.
4. **Refresh**: Click the "Refresh" button in the top-right corner of Grafana.

## 2. Import Dashboard
1. Go to **Dashboards**.
2. Click **New** > **Import**.
3. Upload the `community-dashboard.json` file from this folder.
4. Select the **MySQL** data source you just created.
5. Click **Import**.

## 3. Metrics (Optional)
The app is now exporting Prometheus metrics at `http://localhost:5248/metrics`. 
If you install **Prometheus** locally, you can also add it as a data source to see system performance (CPU, RAM, Request Speed).

---

### Troubleshooting "Inaccessible Site"
If `http://localhost:5248/metrics` says inaccessible:
1. **Is the app running?**: Make sure you have started the application with `dotnet run`.
2. **Check the port**: Your app is configured to use port **5248** (HTTP) or **7053** (HTTPS) in `launchSettings.json`.


### Key SQL Queries used:
- **Total Users**: `SELECT count(*) FROM Users`
- **User Growth**: `SELECT CreatedAt AS time, count(Id) as value FROM Users GROUP BY time ORDER BY time`
- **Community Ranking**: `SELECT c.Name, count(cm.UserId) as MemberCount FROM Communities c LEFT JOIN CommunityMembers cm ON c.Id = cm.CommunityId GROUP BY c.Name ORDER BY MemberCount DESC LIMIT 10`
