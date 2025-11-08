-- Supabase Storage bucket policies for ArtnLove
-- This migration sets up storage buckets (RLS policies managed via Supabase Dashboard)

-- Create a public bucket for artworks (if not exists)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES ('artworks', 'artworks', true, 10485760, ARRAY['image/jpeg', 'image/png', 'image/webp', 'image/avif'])
ON CONFLICT (id) DO NOTHING;

-- Create a private bucket for user uploads (if not exists)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES ('user-uploads', 'user-uploads', false, 10485760, ARRAY['image/jpeg', 'image/png', 'image/webp', 'image/avif'])
ON CONFLICT (id) DO NOTHING;

-- Note: Storage policies should be configured through the Supabase Dashboard
-- under Storage > Policies, or via the Storage API
