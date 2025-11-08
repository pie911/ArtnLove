-- Supabase initial migration for ArtnLove
-- Enables pgcrypto for gen_random_uuid() and creates core tables

-- Ensure pgcrypto is available
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Users
CREATE TABLE IF NOT EXISTS users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email text UNIQUE NOT NULL,
    display_name text,
    metadata jsonb,
    created_at timestamptz DEFAULT now()
);

-- Artworks
CREATE TABLE IF NOT EXISTS artworks (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    owner_id uuid REFERENCES users(id),
    title text NOT NULL,
    description text,
    image_url text NOT NULL,
    visibility text NOT NULL DEFAULT 'public',
    price numeric,
    metadata jsonb,
    created_at timestamptz DEFAULT now()
);

-- Auctions
CREATE TABLE IF NOT EXISTS auctions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    artwork_id uuid NOT NULL REFERENCES artworks(id) ON DELETE CASCADE,
    owner_id uuid NOT NULL REFERENCES users(id),
    start_amount numeric NOT NULL,
    min_increment numeric NOT NULL DEFAULT 1,
    ends_at timestamptz NOT NULL,
    settled boolean DEFAULT false,
    created_at timestamptz DEFAULT now()
);

-- Bids
CREATE TABLE IF NOT EXISTS bids (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    auction_id uuid NOT NULL REFERENCES auctions(id) ON DELETE CASCADE,
    bidder_id uuid NOT NULL REFERENCES users(id),
    amount numeric NOT NULL,
    created_at timestamptz DEFAULT now()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_artworks_owner ON artworks(owner_id);
CREATE INDEX IF NOT EXISTS idx_auctions_artwork ON auctions(artwork_id);
