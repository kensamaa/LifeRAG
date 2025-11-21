'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { api, ChatSession, ChatMessage, Document } from '@/lib/api';

export default function ChatPage() {
  const [sessions, setSessions] = useState<ChatSession[]>([]);
  const [currentSession, setCurrentSession] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [documents, setDocuments] = useState<Document[]>([]);
  const [uploading, setUploading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const router = useRouter();

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) {
      router.push('/auth');
      return;
    }
    loadSessions();
    loadDocuments();
  }, []);

  useEffect(() => {
    if (currentSession) {
      loadMessages(currentSession);
    }
  }, [currentSession]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const loadSessions = async () => {
    try {
      const data = await api.getChatSessions();
      setSessions(data);
    } catch (err) {
      console.error('Failed to load sessions', err);
    }
  };

  const loadMessages = async (sessionId: string) => {
    try {
      const data = await api.getChatSession(sessionId);
      setMessages(data.messages);
    } catch (err) {
      console.error('Failed to load messages', err);
    }
  };

  const loadDocuments = async () => {
    try {
      const data = await api.getDocuments();
      setDocuments(data);
    } catch (err) {
      console.error('Failed to load documents', err);
    }
  };

  const createNewSession = async () => {
    try {
      const session = await api.createChatSession(`Chat ${new Date().toLocaleString()}`);
      await loadSessions();
      setCurrentSession(session.id);
    } catch (err) {
      console.error('Failed to create session', err);
    }
  };

  const sendMessage = async () => {
    if (!input.trim() || !currentSession) return;

    try {
      const response = await api.sendMessage(currentSession, input);
      setMessages([...messages, response.userMessage, response.assistantMessage]);
      setInput('');
      await loadSessions();
    } catch (err) {
      console.error('Failed to send message', err);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploading(true);
    try {
      await api.uploadDocument(file);
      await loadDocuments();
    } catch (err) {
      console.error('Failed to upload file', err);
    } finally {
      setUploading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    router.push('/auth');
  };

  return (
    <div className="flex h-screen bg-gray-100">
      <div className="w-64 bg-white border-r flex flex-col">
        <div className="p-4 border-b">
          <h1 className="text-xl font-bold">LifeRAG</h1>
          <button
            onClick={handleLogout}
            className="mt-2 text-sm text-red-600 hover:text-red-800"
          >
            Logout
          </button>
        </div>

        <div className="p-4 border-b">
          <button
            onClick={createNewSession}
            className="w-full py-2 px-4 bg-blue-600 hover:bg-blue-700 text-white rounded-md text-sm"
          >
            New Chat
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-4">
          <h3 className="text-xs font-semibold text-black mb-2">CHATS</h3>
          {sessions.map((session) => (
            <button
              key={session.id}
              onClick={() => setCurrentSession(session.id)}
              className={`w-full text-left p-2 rounded mb-1 text-sm ${
                currentSession === session.id ? 'bg-blue-100' : 'hover:bg-gray-100'
              }`}
            >
              <div className="font-medium truncate">{session.title}</div>
              <div className="text-xs text-gray-700">{session.messageCount} messages</div>
            </button>
          ))}
        </div>

        <div className="p-4 border-t">
          <h3 className="text-xs font-semibold text-black mb-2">DOCUMENTS ({documents.length})</h3>
          <label className="block">
            <input
              type="file"
              accept=".pdf"
              onChange={handleFileUpload}
              disabled={uploading}
              className="hidden"
            />
            <div className="w-full py-2 px-4 bg-green-600 hover:bg-green-700 text-white rounded-md text-sm text-center cursor-pointer">
              {uploading ? 'Uploading...' : 'Upload PDF'}
            </div>
          </label>
          <div className="mt-2 max-h-32 overflow-y-auto">
            {documents.map((doc) => (
              <div key={doc.id} className="text-xs p-1 truncate" title={doc.fileName}>
                📄 {doc.fileName}
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="flex-1 flex flex-col">
        {currentSession ? (
          <>
            <div className="flex-1 overflow-y-auto p-4 space-y-4">
              {messages.map((msg) => (
                <div
                  key={msg.id}
                  className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}
                >
                  <div
                    className={`max-w-2xl p-3 rounded-lg ${
                      msg.role === 'user'
                        ? 'bg-blue-600 text-white'
                        : 'bg-white border border-gray-200'
                    }`}
                  >
                    <div className="text-sm">{msg.content}</div>
                    <div className="text-xs mt-1 opacity-70">
                      {new Date(msg.createdAt).toLocaleTimeString()}
                    </div>
                  </div>
                </div>
              ))}
              <div ref={messagesEndRef} />
            </div>

            <div className="border-t bg-white p-4">
              <div className="flex gap-2">
                <input
                  type="text"
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && sendMessage()}
                  placeholder="Type your message..."
                  className="flex-1 px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  onClick={sendMessage}
                  className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg"
                >
                  Send
                </button>
              </div>
            </div>
          </>
        ) : (
          <div className="flex-1 flex items-center justify-center text-black">
            <div className="text-center">
              <h2 className="text-2xl font-bold mb-2">Welcome to LifeRAG</h2>
              <p>Create a new chat or select an existing one to start</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
