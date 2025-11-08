-- Supabase Storage bucket policies for ArtnLove
-- This migration sets up proper RLS policies for storage buckets

-- Enable RLS on storage.objects
ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

-- Create a public bucket for artworks (if not exists)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES ('artworks', 'artworks', true, 10485760, ARRAY['image/jpeg', 'image/png', 'image/webp', 'image/avif'])
ON CONFLICT (id) DO NOTHING;

-- Create a private bucket for user uploads (if not exists)
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES ('user-uploads', 'user-uploads', false, 10485760, ARRAY['image/jpeg', 'image/png', 'image/webp', 'image/avif'])
ON CONFLICT (id) DO NOTHING;

-- Policy for public artworks bucket: allow anyone to view
CREATE POLICY "Public artworks are viewable by everyone" ON storage.objects
FOR SELECT USING (bucket_id = 'artworks');

-- Policy for public artworks bucket: allow authenticated users to upload
CREATE POLICY "Authenticated users can upload to artworks" ON storage.objects
FOR INSERT WITH CHECK (
    bucket_id = 'artworks'
    AND auth.role() = 'authenticated'
);

-- Policy for public artworks bucket: allow owners to update/delete their files
CREATE POLICY "Users can update their own artworks" ON storage.objects
FOR UPDATE USING (
    bucket_id = 'artworks'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

CREATE POLICY "Users can delete their own artworks" ON storage.objects
FOR DELETE USING (
    bucket_id = 'artworks'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy for user-uploads bucket: allow authenticated users to view their own files
CREATE POLICY "Users can view their own uploads" ON storage.objects
FOR SELECT USING (
    bucket_id = 'user-uploads'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy for user-uploads bucket: allow authenticated users to upload to their folder
CREATE POLICY "Users can upload to their own folder" ON storage.objects
FOR INSERT WITH CHECK (
    bucket_id = 'user-uploads'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Policy for user-uploads bucket: allow users to update/delete their own files
CREATE POLICY "Users can update their own uploads" ON storage.objects
FOR UPDATE USING (
    bucket_id = 'user-uploads'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

CREATE POLICY "Users can delete their own uploads" ON storage.objects
FOR DELETE USING (
    bucket_id = 'user-uploads'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Grant necessary permissions to authenticated users
GRANT ALL ON storage.objects TO authenticated;
GRANT ALL ON storage.buckets TO authenticated;

-- Grant usage on storage schema
GRANT USAGE ON SCHEMA storage TO authenticated;
GRANT USAGE ON SCHEMA storage TO anon;
