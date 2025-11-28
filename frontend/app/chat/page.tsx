'use client';

import { useState, useEffect, useRef } from 'react';
import { useRouter } from 'next/navigation';
import { api, ChatSession, ChatMessage, Document } from '@/lib/api';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { toast } from 'sonner';
import { useTheme } from 'next-themes';

// Icons
const MenuIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><line x1="4" x2="20" y1="12" y2="12"/><line x1="4" x2="20" y1="6" y2="6"/><line x1="4" x2="20" y1="18" y2="18"/></svg>
);
const PlusIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12h14"/><path d="M12 5v14"/></svg>
);
const SendIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m22 2-7 20-4-9-9-4Z"/><path d="M22 2 11 13"/></svg>
);
const UploadIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" x2="12" y1="3" y2="15"/></svg>
);
const FileIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M14.5 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7.5L14.5 2z"/><polyline points="14 2 14 8 20 8"/></svg>
);
const LogOutIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" x2="9" y1="12" y2="12"/></svg>
);
const XIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>
);
const SunIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="4"/><path d="M12 2v2"/><path d="M12 20v2"/><path d="m4.93 4.93 1.41 1.41"/><path d="m17.66 17.66 1.41 1.41"/><path d="M2 12h2"/><path d="M20 12h2"/><path d="m6.34 17.66-1.41 1.41"/><path d="m19.07 4.93-1.41 1.41"/></svg>
);
const MoonIcon = () => (
  <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z"/></svg>
);

export default function ChatPage() {
  const [sessions, setSessions] = useState<ChatSession[]>([]);
  const [currentSession, setCurrentSession] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [documents, setDocuments] = useState<Document[]>([]);
  const [uploading, setUploading] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const router = useRouter();
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
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
      // Close sidebar on mobile when selecting a session
      if (window.innerWidth < 768) {
        setIsSidebarOpen(false);
      }
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
      toast.error('Failed to load chat sessions');
    }
  };

  const loadMessages = async (sessionId: string) => {
    try {
      const data = await api.getChatSession(sessionId);
      setMessages(data.messages);
    } catch (err) {
      console.error('Failed to load messages', err);
      toast.error('Failed to load messages');
    }
  };

  const loadDocuments = async () => {
    try {
      const data = await api.getDocuments();
      setDocuments(data);
    } catch (err) {
      console.error('Failed to load documents', err);
      toast.error('Failed to load documents');
    }
  };

  const createNewSession = async () => {
    try {
      const session = await api.createChatSession(`Chat ${new Date().toLocaleString()}`);
      await loadSessions();
      setCurrentSession(session.id);
      if (window.innerWidth < 768) {
        setIsSidebarOpen(false);
      }
    } catch (err) {
      console.error('Failed to create session', err);
      toast.error('Failed to create new chat session');
    }
  };

  const sendMessage = async () => {
    if (!input.trim() || !currentSession) return;

    const userMessageContent = input;
    setInput('');
    
    const optimisticUserMessage: ChatMessage = {
      id: `temp-${Date.now()}`,
      role: 'user',
      content: userMessageContent,
      createdAt: new Date().toISOString()
    };
    setMessages([...messages, optimisticUserMessage]);
    setIsLoading(true);

    try {
      const response = await api.sendMessage(currentSession, userMessageContent);
      setMessages(prev => [
        ...prev.filter(m => m.id !== optimisticUserMessage.id),
        response.userMessage,
        response.assistantMessage
      ]);
      await loadSessions();
    } catch (err) {
      console.error('Failed to send message', err);
      toast.error('Failed to send message');
      setMessages(prev => prev.filter(m => m.id !== optimisticUserMessage.id));
    } finally {
      setIsLoading(false);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setUploading(true);
    try {
      await api.uploadDocument(file);
      await loadDocuments();
      toast.success('Document uploaded successfully');
    } catch (err) {
      console.error('Failed to upload file', err);
      toast.error('Failed to upload document');
    } finally {
      setUploading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    router.push('/auth');
  };

  const toggleTheme = () => {
    setTheme(theme === 'dark' ? 'light' : 'dark');
  };

  return (
    <div className="flex h-screen bg-background text-foreground overflow-hidden">
      {/* Mobile Sidebar Overlay */}
      {isSidebarOpen && (
        <div 
          className="fixed inset-0 bg-black/50 z-40 md:hidden backdrop-blur-sm transition-opacity"
          onClick={() => setIsSidebarOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside 
        className={`
          fixed md:static inset-y-0 left-0 z-50 w-72 bg-secondary/50 border-r border-border backdrop-blur-xl
          transform transition-transform duration-300 ease-in-out
          ${isSidebarOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'}
          flex flex-col
        `}
      >
        <div className="p-4 border-b border-border flex items-center justify-between">
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-bold bg-gradient-to-r from-primary to-blue-400 bg-clip-text text-transparent">
              LifeRAG
            </h1>
            {mounted && (
              <button
                onClick={toggleTheme}
                className="p-1.5 rounded-full hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
                title="Toggle Theme"
              >
                {theme === 'dark' ? <SunIcon /> : <MoonIcon />}
              </button>
            )}
          </div>
          <button 
            onClick={() => setIsSidebarOpen(false)}
            className="md:hidden p-2 hover:bg-muted rounded-full text-muted-foreground"
          >
            <XIcon />
          </button>
        </div>

        <div className="p-4">
          <button
            onClick={createNewSession}
            className="w-full py-3 px-4 bg-primary hover:bg-blue-600 text-primary-foreground rounded-xl shadow-lg shadow-blue-500/20 transition-all flex items-center justify-center gap-2 font-medium"
          >
            <PlusIcon />
            New Chat
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-3 py-2 space-y-1">
          <div className="text-xs font-semibold text-muted-foreground px-3 mb-2 uppercase tracking-wider">Recent Chats</div>
          {sessions.map((session) => (
            <button
              key={session.id}
              onClick={() => setCurrentSession(session.id)}
              className={`w-full text-left p-3 rounded-lg text-sm transition-all duration-200 group ${
                currentSession === session.id 
                  ? 'bg-accent text-accent-foreground shadow-sm' 
                  : 'hover:bg-muted text-muted-foreground hover:text-foreground'
              }`}
            >
              <div className="font-medium truncate">{session.title}</div>
              <div className="text-xs opacity-70 mt-1 flex items-center gap-1">
                <span>{session.messageCount} messages</span>
              </div>
            </button>
          ))}
        </div>

        <div className="p-4 border-t border-border bg-secondary/30">
          <div className="mb-4">
            <div className="flex items-center justify-between mb-2 px-1">
              <h3 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Documents</h3>
              <span className="text-xs text-muted-foreground bg-muted px-2 py-0.5 rounded-full">{documents.length}</span>
            </div>
            
            <div className="space-y-1 max-h-32 overflow-y-auto mb-3 custom-scrollbar">
              {documents.map((doc) => (
                <div key={doc.id} className="flex items-center gap-2 text-xs p-2 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors" title={doc.fileName}>
                  <FileIcon />
                  <span className="truncate">{doc.fileName}</span>
                </div>
              ))}
            </div>

            <label className="block">
              <input
                type="file"
                accept=".pdf"
                onChange={handleFileUpload}
                disabled={uploading}
                className="hidden"
              />
              <div className="w-full py-2 px-4 border border-dashed border-border hover:border-primary hover:bg-accent/50 text-muted-foreground hover:text-accent-foreground rounded-lg text-sm text-center cursor-pointer transition-all flex items-center justify-center gap-2">
                <UploadIcon />
                {uploading ? 'Uploading...' : 'Upload PDF'}
              </div>
            </label>
          </div>

          <button
            onClick={handleLogout}
            className="w-full flex items-center justify-center gap-2 px-3 py-2 text-sm text-muted-foreground hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/30 rounded-lg transition-colors"
          >
            <LogOutIcon />
            Sign Out
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 flex flex-col relative w-full">
        {/* Header */}
        <header className="h-16 border-b border-border flex items-center px-4 justify-between md:justify-end bg-background/80 backdrop-blur-md sticky top-0 z-10">
          <button 
            onClick={() => setIsSidebarOpen(true)}
            className="md:hidden p-2 -ml-2 text-muted-foreground hover:text-foreground"
          >
            <MenuIcon />
          </button>
          <div className="md:hidden font-semibold flex items-center gap-2">
            LifeRAG
            {mounted && (
              <button
                onClick={toggleTheme}
                className="p-1.5 rounded-full hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
                title="Toggle Theme"
              >
                {theme === 'dark' ? <SunIcon /> : <MoonIcon />}
              </button>
            )}
          </div>
          <div className="w-8 md:w-0"></div> {/* Spacer for centering */}
        </header>

        {currentSession ? (
          <>
            <div className="flex-1 overflow-y-auto p-4 md:p-6 space-y-6 scroll-smooth">
              {messages.map((msg) => (
                <div
                  key={msg.id}
                  className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'} animate-in fade-in slide-in-from-bottom-2 duration-300`}
                >
                  <div
                    className={`max-w-[85%] md:max-w-2xl p-4 rounded-2xl shadow-sm ${
                      msg.role === 'user'
                        ? 'bg-primary text-primary-foreground rounded-br-none'
                        : 'bg-card border border-border text-card-foreground rounded-bl-none'
                    }`}
                  >
                    <div className="text-sm md:text-base prose prose-sm dark:prose-invert max-w-none break-words">
                      <ReactMarkdown remarkPlugins={[remarkGfm]}>
                        {msg.content}
                      </ReactMarkdown>
                    </div>
                    <div className={`text-[10px] mt-2 ${msg.role === 'user' ? 'text-blue-100' : 'text-muted-foreground'}`}>
                      {new Date(msg.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </div>
                  </div>
                </div>
              ))}
              {isLoading && (
                <div className="flex justify-start animate-in fade-in duration-300">
                  <div className="bg-card border border-border p-4 rounded-2xl rounded-bl-none shadow-sm">
                    <div className="flex items-center gap-1.5">
                      <div className="w-2 h-2 bg-primary/50 rounded-full animate-bounce" style={{ animationDelay: '0s' }} />
                      <div className="w-2 h-2 bg-primary/50 rounded-full animate-bounce" style={{ animationDelay: '0.15s' }} />
                      <div className="w-2 h-2 bg-primary/50 rounded-full animate-bounce" style={{ animationDelay: '0.3s' }} />
                    </div>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            <div className="p-4 md:p-6 bg-background/80 backdrop-blur-md border-t border-border sticky bottom-0 z-10">
              <div className="max-w-4xl mx-auto relative">
                <input
                  type="text"
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && sendMessage()}
                  placeholder="Ask anything..."
                  className="w-full pl-6 pr-14 py-4 bg-secondary/50 border border-border rounded-full focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all shadow-sm text-foreground placeholder:text-muted-foreground"
                />
                <button
                  onClick={sendMessage}
                  disabled={isLoading || !input.trim()}
                  className="absolute right-2 top-2 p-2 bg-primary text-primary-foreground rounded-full hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-all shadow-md hover:shadow-lg active:scale-95"
                >
                  <SendIcon />
                </button>
              </div>
              <div className="text-center mt-2">
                <p className="text-[10px] text-muted-foreground">AI can make mistakes. Check important info.</p>
              </div>
            </div>
          </>
        ) : (
          <div className="flex-1 flex flex-col items-center justify-center p-8 text-center animate-in fade-in zoom-in duration-500">
            <div className="w-16 h-16 bg-primary/10 rounded-2xl flex items-center justify-center mb-6 text-primary">
              <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
            </div>
            <h2 className="text-3xl font-bold mb-3 bg-gradient-to-r from-foreground to-muted-foreground bg-clip-text text-transparent">
              Welcome to LifeRAG
            </h2>
            <p className="text-muted-foreground max-w-md mb-8">
              Your personal AI assistant powered by your own documents. Upload a PDF or start a new chat to begin.
            </p>
            <button
              onClick={createNewSession}
              className="px-8 py-3 bg-primary hover:bg-blue-600 text-primary-foreground rounded-full font-medium shadow-lg shadow-blue-500/25 transition-all hover:-translate-y-0.5"
            >
              Start a Conversation
            </button>
          </div>
        )}
      </main>
    </div>
  );
}
