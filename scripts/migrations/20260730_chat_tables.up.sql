-- =====================================================
-- Migration: Create Chat Tables
-- Created: 2026-07-30
-- =====================================================

-- CHAT_CONVERSATIONS
CREATE TABLE chat_conversations (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    type NVARCHAR(20) NOT NULL, -- DIRECT, SUPPORT, GROUP
    title NVARCHAR(255) NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE', -- ACTIVE, CLOSED
    created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    closed_at DATETIME2 NULL
);

CREATE INDEX ix_chat_conversations_type ON chat_conversations(type);
CREATE INDEX ix_chat_conversations_status ON chat_conversations(status);

-- CHAT_PARTICIPANTS
CREATE TABLE chat_participants (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    conversation_id UNIQUEIDENTIFIER NOT NULL,
    user_id UNIQUEIDENTIFIER NOT NULL,
    role NVARCHAR(20) NOT NULL, -- CUSTOMER, SUPPORT, ADMIN
    joined_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    last_read_at DATETIME2 NULL,
    CONSTRAINT fk_cp_conversation FOREIGN KEY (conversation_id) 
        REFERENCES chat_conversations(id) ON DELETE CASCADE,
    CONSTRAINT fk_cp_user FOREIGN KEY (user_id) 
        REFERENCES users(id) ON DELETE CASCADE
);

CREATE INDEX ix_chat_participants_conversation ON chat_participants(conversation_id);
CREATE INDEX ix_chat_participants_user ON chat_participants(user_id);
CREATE UNIQUE INDEX ix_chat_participants_unique ON chat_participants(conversation_id, user_id);

-- CHAT_MESSAGES
CREATE TABLE chat_messages (
    id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    conversation_id UNIQUEIDENTIFIER NOT NULL,
    sender_id UNIQUEIDENTIFIER NOT NULL,
    content NVARCHAR(MAX) NOT NULL,
    type NVARCHAR(20) NOT NULL DEFAULT 'TEXT', -- TEXT, IMAGE, FILE, SYSTEM
    sent_at DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    read_at DATETIME2 NULL,
    is_pinned BIT NOT NULL DEFAULT 0,
    reply_to_id UNIQUEIDENTIFIER NULL,
    attachment_url NVARCHAR(500) NULL,
    attachment_type NVARCHAR(100) NULL,
    CONSTRAINT fk_cm_conversation FOREIGN KEY (conversation_id) 
        REFERENCES chat_conversations(id) ON DELETE CASCADE,
    CONSTRAINT fk_cm_sender FOREIGN KEY (sender_id) 
        REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_cm_reply FOREIGN KEY (reply_to_id)
        REFERENCES chat_messages(id) ON DELETE NO ACTION
);

CREATE INDEX ix_chat_messages_conversation ON chat_messages(conversation_id);
CREATE INDEX ix_chat_messages_sender ON chat_messages(sender_id);
CREATE INDEX ix_chat_messages_sent_at ON chat_messages(sent_at);
CREATE INDEX ix_chat_messages_reply_to ON chat_messages(reply_to_id);
