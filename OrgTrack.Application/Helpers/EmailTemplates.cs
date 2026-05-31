namespace OrgTrack.Application.Helpers;

public static class EmailTemplates
{
    public static string GetWelcomeEmail(string firstName, string appUrl)
    {
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to OrgTrack</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            background-color: #f3f4f6;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
        }}
        .header {{
            background-color: #10b981; /* Emerald 500 */
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            color: #ffffff;
            margin: 0;
            font-size: 28px;
            font-weight: 700;
        }}
        .content {{
            padding: 40px 30px;
            color: #374151; /* Gray 700 */
        }}
        .content h2 {{
            color: #111827; /* Gray 900 */
            font-size: 20px;
            margin-top: 0;
        }}
        .content p {{
            font-size: 16px;
            margin-bottom: 20px;
        }}
        .features {{
            background-color: #f9fafb;
            border-radius: 8px;
            padding: 20px;
            margin-bottom: 30px;
        }}
        .features ul {{
            margin: 0;
            padding-left: 20px;
        }}
        .features li {{
            margin-bottom: 10px;
            color: #4b5563; /* Gray 600 */
        }}
        .button-container {{
            text-align: center;
            margin: 30px 0;
        }}
        .button {{
            background-color: #10b981;
            color: #ffffff !important;
            text-decoration: none;
            padding: 12px 24px;
            border-radius: 6px;
            font-weight: 600;
            font-size: 16px;
            display: inline-block;
        }}
        .footer {{
            background-color: #f9fafb;
            padding: 20px;
            text-align: center;
            color: #6b7280; /* Gray 500 */
            font-size: 14px;
            border-top: 1px solid #e5e7eb;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>OrgTrack</h1>
        </div>
        <div class=""content"">
            <h2>Welcome aboard, {firstName}! 🚀</h2>
            <p>We're thrilled to have you here. OrgTrack is your all-in-one platform for managing your organization's structure, tasks, events, and members.</p>
            
            <div class=""features"">
                <p><strong>Here's what you can do next:</strong></p>
                <ul>
                    <li>Set up your profile and connect your Google Calendar</li>
                    <li>Explore your organization's structure</li>
                    <li>Check out your Kanban board for assigned tasks</li>
                    <li>RSVP to upcoming events and meetings</li>
                </ul>
            </div>
            
            <div class=""button-container"">
                <a href=""{appUrl}"" class=""button"">Go to Dashboard</a>
            </div>
            
            <p>If you have any questions or need help getting started, simply reply to this email or reach out to your team leader.</p>
            
            <p>Best regards,<br>The OrgTrack Team</p>
        </div>
        <div class=""footer"">
            <p>&copy; {DateTime.UtcNow.Year} OrgTrack. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
