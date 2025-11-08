-- Enhanced schema for ArtnLove with user roles, orders, blogs, and social features

-- Add user roles and profile enhancements
ALTER TABLE users ADD COLUMN IF NOT EXISTS role text DEFAULT 'buyer' CHECK (role IN ('buyer', 'seller', 'admin'));
ALTER TABLE users ADD COLUMN IF NOT EXISTS bio text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS avatar_url text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS location text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS website text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS verified boolean DEFAULT false;
ALTER TABLE users ADD COLUMN IF NOT EXISTS updated_at timestamptz DEFAULT now();

-- Enhance artworks table with more metadata
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS category text;
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS tags text[];
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS dimensions text;
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS medium text;
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS year_created integer;
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS is_available boolean DEFAULT true;
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS views_count integer DEFAULT 0;
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS likes_count integer DEFAULT 0;
ALTER TABLE artworks ADD COLUMN IF NOT EXISTS updated_at timestamptz DEFAULT now();

-- Orders and delivery system
CREATE TABLE IF NOT EXISTS orders (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    buyer_id uuid NOT NULL REFERENCES users(id),
    artwork_id uuid NOT NULL REFERENCES artworks(id),
    seller_id uuid NOT NULL REFERENCES users(id),
    amount numeric NOT NULL,
    status text DEFAULT 'pending' CHECK (status IN ('pending', 'paid', 'shipped', 'delivered', 'cancelled')),
    shipping_address jsonb,
    tracking_number text,
    notes text,
    created_at timestamptz DEFAULT now(),
    updated_at timestamptz DEFAULT now()
);

-- Delivery tracking
CREATE TABLE IF NOT EXISTS delivery_updates (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id uuid NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    status text NOT NULL,
    message text,
    location text,
    created_at timestamptz DEFAULT now()
);

-- Blog posts
CREATE TABLE IF NOT EXISTS blog_posts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    author_id uuid NOT NULL REFERENCES users(id),
    title text NOT NULL,
    content text NOT NULL,
    excerpt text,
    featured_image_url text,
    tags text[],
    published boolean DEFAULT false,
    published_at timestamptz,
    views_count integer DEFAULT 0,
    likes_count integer DEFAULT 0,
    created_at timestamptz DEFAULT now(),
    updated_at timestamptz DEFAULT now()
);

-- Social features
CREATE TABLE IF NOT EXISTS likes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id),
    artwork_id uuid REFERENCES artworks(id),
    blog_post_id uuid REFERENCES blog_posts(id),
    created_at timestamptz DEFAULT now(),
    UNIQUE(user_id, artwork_id),
    UNIQUE(user_id, blog_post_id)
);

CREATE TABLE IF NOT EXISTS comments (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id),
    artwork_id uuid REFERENCES artworks(id),
    blog_post_id uuid REFERENCES blog_posts(id),
    parent_comment_id uuid REFERENCES comments(id),
    content text NOT NULL,
    created_at timestamptz DEFAULT now(),
    updated_at timestamptz DEFAULT now()
);

CREATE TABLE IF NOT EXISTS follows (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    follower_id uuid NOT NULL REFERENCES users(id),
    following_id uuid NOT NULL REFERENCES users(id),
    created_at timestamptz DEFAULT now(),
    UNIQUE(follower_id, following_id),
    CHECK (follower_id != following_id)
);

-- Notifications
CREATE TABLE IF NOT EXISTS notifications (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id),
    type text NOT NULL CHECK (type IN ('like', 'comment', 'follow', 'bid', 'sale', 'order_update')),
    title text NOT NULL,
    message text NOT NULL,
    data jsonb,
    read boolean DEFAULT false,
    created_at timestamptz DEFAULT now()
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_orders_buyer ON orders(buyer_id);
CREATE INDEX IF NOT EXISTS idx_orders_seller ON orders(seller_id);
CREATE INDEX IF NOT EXISTS idx_orders_artwork ON orders(artwork_id);
CREATE INDEX IF NOT EXISTS idx_blog_posts_author ON blog_posts(author_id);
CREATE INDEX IF NOT EXISTS idx_blog_posts_published ON blog_posts(published, published_at DESC);
CREATE INDEX IF NOT EXISTS idx_likes_artwork ON likes(artwork_id);
CREATE INDEX IF NOT EXISTS idx_likes_blog ON likes(blog_post_id);
CREATE INDEX IF NOT EXISTS idx_comments_artwork ON comments(artwork_id);
CREATE INDEX IF NOT EXISTS idx_comments_blog ON comments(blog_post_id);
CREATE INDEX IF NOT EXISTS idx_follows_follower ON follows(follower_id);
CREATE INDEX IF NOT EXISTS idx_follows_following ON follows(following_id);
CREATE INDEX IF NOT EXISTS idx_notifications_user ON notifications(user_id, read, created_at DESC);
