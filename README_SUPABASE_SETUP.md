# ArtnLove Supabase Setup Guide

## Overview
ArtnLove is an ASP.NET Core art gallery platform that uses Supabase for backend services including database, authentication, and file storage.

## Prerequisites
- .NET 8 SDK
- Supabase CLI (optional, for local development)
- Supabase account and project

## Supabase Configuration

### 1. Project Setup
1. Create a new Supabase project at https://supabase.com
2. Note your project URL and API keys from the project settings

### 2. Environment Variables
Update your `appsettings.json` or environment variables with your Supabase credentials:

```json
{
  "Supabase": {
    "Url": "https://your-project-ref.supabase.co",
    "AnonKey": "your-anon-key",
    "ServiceRoleKey": "your-service-role-key"
  }
}
```

### 3. Database Setup
Run the migrations in order:

1. `supabase/migrations/0001_initial.sql` - Creates core tables
2. `supabase/migrations/0002_bucket_policies.sql` - Sets up storage buckets and policies
3. `supabase/migrations/0003_user_permissions.sql` - Configures RLS policies

To apply migrations:
```bash
supabase db push
```

Or manually execute the SQL files in your Supabase SQL editor.

### 4. Storage Buckets
The application uses two storage buckets:
- `artworks` - Public bucket for artwork images
- `user-uploads` - Private bucket for user uploads

Both buckets are configured with appropriate RLS policies.

### 5. Authentication
The app uses Supabase Auth with JWT tokens. Configure your auth settings in Supabase:
- Enable email/password authentication
- Configure redirect URLs for your domain
- Set up any additional auth providers if needed

## Running the Application

1. Build and run the .NET application:
```bash
cd ArtnLove
dotnet build
dotnet run
```

2. Access the application at `http://localhost:5068`

## API Endpoints

- `GET /api/v1/config` - Returns Supabase configuration for frontend
- `GET /api/v1/artworks` - Fetch artworks with pagination
- `POST /api/v1/artworks` - Create new artwork
- `POST /api/v1/storage/presign` - Generate signed upload URLs

## Frontend Pages

- `/` - Landing page with hero section and features
- `/client/index.html` - Gallery view
- `/client/upload.html` - Upload artwork form
- `/client/profile.html` - User profile page

## Troubleshooting

### Storage Upload Issues
- Ensure buckets exist and have correct policies
- Check that the service role key has storage permissions
- Verify bucket names match between frontend and backend

### Authentication Issues
- Confirm JWT middleware is configured correctly
- Check that auth policies are applied to protected routes
- Verify Supabase auth settings match your application requirements

### Database Connection
- Ensure DATABASE_URL environment variable is set for production
- Check that RLS policies allow necessary operations
- Verify table permissions are correctly configured

## Security Notes

- Service role keys should never be exposed to the frontend
- RLS policies ensure users can only access their own data
- Storage policies control file access based on ownership
- Rate limiting is implemented on upload endpoints

## Development

For local development with Supabase CLI:
```bash
supabase start
# This starts local Supabase services
```

Update your configuration to use local URLs when developing locally.
