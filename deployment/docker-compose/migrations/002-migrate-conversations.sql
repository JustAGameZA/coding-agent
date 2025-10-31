-- 002-migrate-conversations.sql
-- Migrate legacy conversations/messages into chat schema

-- Conversations
INSERT INTO chat.conversations (id, user_id, title, status, created_at, updated_at, metadata)
SELECT c.id, c.user_id, c.title, COALESCE(c.status, 'active'), COALESCE(c.created_at, NOW()), COALESCE(c.updated_at, NOW()), c.metadata
FROM legacy.conversations c
LEFT JOIN chat.conversations cc ON cc.id = c.id
WHERE cc.id IS NULL;

-- Messages
INSERT INTO chat.messages (id, conversation_id, sender, content, message_type, created_at, metadata)
SELECT m.id, m.conversation_id, COALESCE(m.sender, 'user'), m.content, COALESCE(m.message_type, 'text'), COALESCE(m.created_at, NOW()), m.metadata
FROM legacy.messages m
LEFT JOIN chat.messages cm ON cm.id = m.id
WHERE cm.id IS NULL;
