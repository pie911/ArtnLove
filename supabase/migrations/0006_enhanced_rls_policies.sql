-- Enhanced RLS policies for the expanded schema

-- Enable RLS on new tables
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE delivery_updates ENABLE ROW LEVEL SECURITY;
ALTER TABLE blog_posts ENABLE ROW LEVEL SECURITY;
ALTER TABLE likes ENABLE ROW LEVEL SECURITY;
ALTER TABLE comments ENABLE ROW LEVEL SECURITY;
ALTER TABLE follows ENABLE ROW LEVEL SECURITY;
ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;

-- Orders policies
CREATE POLICY "Users can view their own orders as buyer or seller" ON orders
FOR SELECT USING (auth.uid() = buyer_id OR auth.uid() = seller_id);

CREATE POLICY "Buyers can create orders" ON orders
FOR INSERT WITH CHECK (auth.uid() = buyer_id);

CREATE POLICY "Buyers and sellers can update their orders" ON orders
FOR UPDATE USING (auth.uid() = buyer_id OR auth.uid() = seller_id);

-- Delivery updates policies
CREATE POLICY "Users can view delivery updates for their orders" ON delivery_updates
FOR SELECT USING (
    EXISTS (
        SELECT 1 FROM orders
        WHERE orders.id = delivery_updates.order_id
        AND (orders.buyer_id = auth.uid() OR orders.seller_id = auth.uid())
    )
);

CREATE POLICY "Sellers can create delivery updates for their orders" ON delivery_updates
FOR INSERT WITH CHECK (
    EXISTS (
        SELECT 1 FROM orders
        WHERE orders.id = delivery_updates.order_id
        AND orders.seller_id = auth.uid()
    )
);

-- Blog posts policies
CREATE POLICY "Published blog posts are viewable by everyone" ON blog_posts
FOR SELECT USING (published = true);

CREATE POLICY "Authors can view their own unpublished posts" ON blog_posts
FOR SELECT USING (auth.uid() = author_id);

CREATE POLICY "Authenticated users can create blog posts" ON blog_posts
FOR INSERT WITH CHECK (auth.uid() = author_id);

CREATE POLICY "Authors can update their own posts" ON blog_posts
FOR UPDATE USING (auth.uid() = author_id);

CREATE POLICY "Authors can delete their own posts" ON blog_posts
FOR DELETE USING (auth.uid() = author_id);

-- Likes policies
CREATE POLICY "Likes are viewable by everyone" ON likes
FOR SELECT TO authenticated USING (true);

CREATE POLICY "Authenticated users can create likes" ON likes
FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can delete their own likes" ON likes
FOR DELETE USING (auth.uid() = user_id);

-- Comments policies
CREATE POLICY "Comments are viewable by everyone" ON comments
FOR SELECT TO authenticated USING (true);

CREATE POLICY "Authenticated users can create comments" ON comments
FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update their own comments" ON comments
FOR UPDATE USING (auth.uid() = user_id);

CREATE POLICY "Users can delete their own comments" ON comments
FOR DELETE USING (auth.uid() = user_id);

-- Follows policies
CREATE POLICY "Follows are viewable by everyone" ON follows
FOR SELECT TO authenticated USING (true);

CREATE POLICY "Authenticated users can create follows" ON follows
FOR INSERT WITH CHECK (auth.uid() = follower_id);

CREATE POLICY "Users can delete their own follows" ON follows
FOR DELETE USING (auth.uid() = follower_id);

-- Notifications policies
CREATE POLICY "Users can view their own notifications" ON notifications
FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Users can update their own notifications" ON notifications
FOR UPDATE USING (auth.uid() = user_id);

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON orders TO authenticated;
GRANT SELECT, INSERT ON delivery_updates TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON blog_posts TO authenticated;
GRANT SELECT, INSERT, DELETE ON likes TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON comments TO authenticated;
GRANT SELECT, INSERT, DELETE ON follows TO authenticated;
GRANT SELECT, UPDATE ON notifications TO authenticated;

-- Grant usage
GRANT USAGE ON SCHEMA public TO authenticated;
GRANT USAGE ON SCHEMA public TO anon;
