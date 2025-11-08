-- Storage policies for ArtnLove buckets
-- Enable RLS on storage.objects
ALTER TABLE storage.objects ENABLE ROW LEVEL SECURITY;

-- Policies for artworks bucket (public)
-- Allow anyone to view public artworks
CREATE POLICY "Public artworks are viewable by everyone" ON storage.objects
FOR SELECT USING (bucket_id = 'artworks');

-- Allow authenticated users to upload to artworks bucket
CREATE POLICY "Authenticated users can upload to artworks" ON storage.objects
FOR INSERT WITH CHECK (
    bucket_id = 'artworks'
    AND auth.role() = 'authenticated'
);

-- Allow users to update their own artworks
CREATE POLICY "Users can update their own artworks" ON storage.objects
FOR UPDATE USING (
    bucket_id = 'artworks'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Allow users to delete their own artworks
CREATE POLICY "Users can delete their own artworks" ON storage.objects
FOR DELETE USING (
    bucket_id = 'artworks'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Policies for user-uploads bucket (private)
-- Allow authenticated users to view their own uploads
CREATE POLICY "Users can view their own uploads" ON storage.objects
FOR SELECT USING (
    bucket_id = 'user-uploads'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Allow authenticated users to upload to their own folder
CREATE POLICY "Users can upload to their own folder" ON storage.objects
FOR INSERT WITH CHECK (
    bucket_id = 'user-uploads'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Allow users to update their own uploads
CREATE POLICY "Users can update their own uploads" ON storage.objects
FOR UPDATE USING (
    bucket_id = 'user-uploads'
    AND auth.uid()::text = (storage.foldername(name))[1]
);

-- Allow users to delete their own uploads
CREATE POLICY "Users can delete their own uploads" ON storage.objects
FOR DELETE USING (
    bucket_id = 'user-uploads'
    AND auth.uid()::text = (storage.foldername(name))[1]
);
