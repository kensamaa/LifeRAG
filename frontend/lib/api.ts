const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5104';

export interface AuthResponse {
  token: string;
  userId: string;
  email: string;
}

export interface ChatSession {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string | null;
  messageCount: number;
}

export interface ChatMessage {
  id: string;
  role: string;
  content: string;
  createdAt: string;
}

export interface Document {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  uploadedAt: string;
}

class ApiClient {
  private getToken(): string | null {
    if (typeof window !== 'undefined') {
      return localStorage.getItem('token');
    }
    return null;
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const token = this.getToken();
    const headers: Record<string, string> = {
      ...(options.headers as Record<string, string>),
    };

    if (token && !headers['Authorization']) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    if (!(options.body instanceof FormData) && !headers['Content-Type']) {
      headers['Content-Type'] = 'application/json';
    }

    const response = await fetch(`${API_URL}${endpoint}`, {
      ...options,
      headers,
    });

    if (!response.ok) {
      throw new Error(`API Error: ${response.statusText}`);
    }

    return response.json();
  }

  async register(email: string, password: string, fullName: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, fullName }),
    });
  }

  async login(email: string, password: string): Promise<AuthResponse> {
    return this.request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  }

  async getChatSessions(): Promise<ChatSession[]> {
    return this.request<ChatSession[]>('/api/chat/sessions');
  }

  async createChatSession(title: string): Promise<{ id: string; title: string; createdAt: string }> {
    return this.request('/api/chat/sessions', {
      method: 'POST',
      body: JSON.stringify({ title }),
    });
  }

  async getChatSession(sessionId: string): Promise<{ id: string; title: string; createdAt: string; updatedAt: string | null; messages: ChatMessage[] }> {
    return this.request(`/api/chat/sessions/${sessionId}`);
  }

  async sendMessage(sessionId: string, content: string): Promise<{ userMessage: ChatMessage; assistantMessage: ChatMessage }> {
    return this.request(`/api/chat/sessions/${sessionId}/messages`, {
      method: 'POST',
      body: JSON.stringify({ content }),
    });
  }

  async deleteChatSession(sessionId: string): Promise<void> {
    await fetch(`${API_URL}/api/chat/sessions/${sessionId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${this.getToken()}`,
      },
    });
  }

  async uploadDocument(file: File): Promise<Document> {
    const formData = new FormData();
    formData.append('file', file);

    const token = this.getToken();
    const response = await fetch(`${API_URL}/api/documents/upload`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
      },
      body: formData,
    });

    if (!response.ok) {
      throw new Error(`Upload failed: ${response.statusText}`);
    }

    return response.json();
  }

  async getDocuments(): Promise<Document[]> {
    return this.request<Document[]>('/api/documents');
  }

  async deleteDocument(id: string): Promise<void> {
    await fetch(`${API_URL}/api/documents/${id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${this.getToken()}`,
      },
    });
  }
}

export const api = new ApiClient();
