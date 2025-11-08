-- User permissions and RLS policies for ArtnLove
-- This migration sets up proper Row Level Security policies for all tables

-- Enable RLS on all tables
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE artworks ENABLE ROW LEVEL SECURITY;
ALTER TABLE auctions ENABLE ROW LEVEL SECURITY;
ALTER TABLE bids ENABLE ROW LEVEL SECURITY;

-- Users table policies
-- Users can view their own profile
CREATE POLICY "Users can view their own profile" ON users
FOR SELECT USING (auth.uid() = id);

-- Users can update their own profile
CREATE POLICY "Users can update their own profile" ON users
FOR UPDATE USING (auth.uid() = id);

-- Allow users to insert their own profile (for registration)
CREATE POLICY "Users can create their own profile" ON users
FOR INSERT WITH CHECK (auth.uid() = id);

-- Artworks table policies
-- Public artworks are viewable by everyone
CREATE POLICY "Public artworks are viewable by everyone" ON artworks
FOR SELECT USING (visibility = 'public');

-- Users can view their own artworks (including private ones)
CREATE POLICY "Users can view their own artworks" ON artworks
FOR SELECT USING (auth.uid() = owner_id);

-- Authenticated users can create artworks
CREATE POLICY "Authenticated users can create artworks" ON artworks
FOR INSERT WITH CHECK (auth.uid() = owner_id);

-- Users can update their own artworks
CREATE POLICY "Users can update their own artworks" ON artworks
FOR UPDATE USING (auth.uid() = owner_id);

-- Users can delete their own artworks
CREATE POLICY "Users can delete their own artworks" ON artworks
FOR DELETE USING (auth.uid() = owner_id);

-- Auctions table policies
-- Everyone can view auctions
CREATE POLICY "Auctions are viewable by everyone" ON auctions
FOR SELECT TO authenticated USING (true);

-- Only artwork owners can create auctions
CREATE POLICY "Artwork owners can create auctions" ON auctions
FOR INSERT WITH CHECK (auth.uid() = owner_id);

-- Only auction owners can update auctions
CREATE POLICY "Auction owners can update auctions" ON auctions
FOR UPDATE USING (auth.uid() = owner_id);

-- Only auction owners can delete auctions (before settlement)
CREATE POLICY "Auction owners can delete unsettled auctions" ON auctions
FOR DELETE USING (auth.uid() = owner_id AND settled = false);

-- Bids table policies
-- Everyone can view bids on auctions
CREATE POLICY "Bids are viewable by everyone" ON bids
FOR SELECT TO authenticated USING (true);

-- Authenticated users can place bids
CREATE POLICY "Authenticated users can place bids" ON bids
FOR INSERT WITH CHECK (auth.uid() = bidder_id);

-- Users can update their own bids (if allowed by business logic)
CREATE POLICY "Users can update their own bids" ON bids
FOR UPDATE USING (auth.uid() = bidder_id);

-- Users can delete their own bids (if allowed by business logic)
CREATE POLICY "Users can delete their own bids" ON bids
FOR DELETE USING (auth.uid() = bidder_id);

-- Grant necessary permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON users TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON artworks TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON auctions TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON bids TO authenticated;

-- Grant usage on schema
GRANT USAGE ON SCHEMA public TO authenticated;
GRANT USAGE ON SCHEMA public TO anon;
