#!/bin/bash

# Build the generic Chat.tsx
cat << 'INNER_EOF' > /tmp/Chat.tsx
import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { Send, Search, MoreVertical, Phone, Video } from 'lucide-react';
import { format } from 'date-fns';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import toast from 'react-hot-toast';

export default function Chat() {
  const { user } = useAuth();
  const [searchParams] = useSearchParams();
  const preSelectedUserId = searchParams.get('userId');
  
  const [activeChatId, setActiveChatId] = useState<number | null>(preSelectedUserId ? parseInt(preSelectedUserId) : null);
  const [messageInput, setMessageInput] = useState('');
  const [messages, setMessages] = useState<any[]>([]);
  const [conversations, setConversations] = useState<any[]>([]);
  const [onlineUsers, setOnlineUsers] = useState<number[]>([]);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const activeConversation = conversations.find(c => c.id === activeChatId);

  useEffect(() => {
    const fetchConversations = async () => {
      try {
        const token = localStorage.getItem('tourmate_token');
        const res = await fetch('http://localhost:5066/api/messages/conversations', {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (res.ok) {
          const data = await res.json();
          setConversations(data);
          if (data.length > 0 && !activeChatId && !preSelectedUserId) {
            setActiveChatId(data[0].id);
          }
        }
      } catch (err) {
        console.error(err);
      }
    };
    fetchConversations();

    const token = localStorage.getItem('tourmate_token');
    if (token) {
      const newConnection = new HubConnectionBuilder()
        .withUrl('http://localhost:5066/chathub', {
          accessTokenFactory: () => token
        })
        .configureLogging(LogLevel.Information)
        .withAutomaticReconnect()
        .build();

      setConnection(newConnection);
    }
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          connection.invoke('GetOnlineUsers').then((users: number[]) => {
            setOnlineUsers(users);
          });

          connection.on('UserOnline', (userId: number) => {
            setOnlineUsers(prev => [...new Set([...prev, userId])]);
          });

          connection.on('UserOffline', (userId: number) => {
            setOnlineUsers(prev => prev.filter(id => id !== userId));
          });
          
          connection.on('ReceiveMessage', (message) => {
            setMessages(prev => {
              if (!prev.find(m => m.id === message.id)) {
                return [...prev, message];
              }
              return prev;
            });
            scrollToBottom();
          });
        })
        .catch(e => console.log('Connection failed: ', e));

      return () => {
        connection.off('UserOnline');
        connection.off('UserOffline');
        connection.off('ReceiveMessage');
        connection.stop();
      };
    }
  }, [connection]);

  useEffect(() => {
    if (!activeChatId) return;

    const fetchMessages = async () => {
      try {
        const token = localStorage.getItem('tourmate_token');
        const res = await fetch(`http://localhost:5066/api/messages/${activeChatId}`, {
          headers: { Authorization: `Bearer ${token}` }
        });
        if (res.ok) {
          const data = await res.json();
          setMessages(data);
          scrollToBottom();
        }
      } catch (err) {
        console.error(err);
      }
    };

    fetchMessages();
  }, [activeChatId]);

  const scrollToBottom = () => {
    setTimeout(() => {
      messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!messageInput.trim() || !activeChatId || !connection) return;

    try {
      await connection.invoke('SendMessage', activeChatId, messageInput);
      setMessageInput('');
      scrollToBottom();
    } catch (e) {
      console.error(e);
      toast.error('Failed to send message');
    }
  };

  const currentMessages = messages.filter(
    m => (m.senderId === user?.id && m.receiverId === activeChatId) || 
         (m.senderId === activeChatId && m.receiverId === user?.id)
  ).sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime());

  return (
    <div className="p-6 lg:p-8 h-[calc(100vh-64px)]">
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden h-full flex">
        <div className="w-full md:w-80 border-r border-gray-100 flex flex-col h-full">
          <div className="p-4 border-b border-gray-100">
            <div className="relative">
              <input
                type="text"
                placeholder="Search messages..."
                className="w-full pl-10 pr-4 py-2 bg-gray-50 border-none rounded-xl focus:ring-2 focus:ring-forest-500/20 text-sm"
              />
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-gray-400" />
            </div>
          </div>
          
          <div className="flex-1 overflow-y-auto">
            {conversations.map(c => (
              <button
                key={c.id}
                onClick={() => setActiveChatId(c.id)}
                className={`w-full p-4 flex items-center gap-3 hover:bg-gray-50 transition-colors ${
                  activeChatId === c.id ? 'bg-forest-600/5 border-r-4 border-forest-600' : ''
                }`}
              >
                <div className="relative">
                  <img
                    src={c.avatar || `https://ui-avatars.com/api/?name=${c.name}&background=CCC&color=fff`}
                    alt={c.name}
                    className="w-12 h-12 rounded-full object-cover border border-gray-200"
                  />
                  {onlineUsers.includes(c.id) && (
                    <div className="absolute bottom-0 right-0 w-3 h-3 bg-green-500 border-2 border-white rounded-full"></div>
                  )}
                </div>
                <div className="flex-1 text-left min-w-0">
                  <div className="flex justify-between items-baseline mb-1">
                    <h3 className="font-semibold text-gray-900 truncate">{c.name}</h3>
                  </div>
                  <p className="text-sm text-gray-500 truncate capitalize">
                    {c.role}
                  </p>
                </div>
              </button>
            ))}
            {conversations.length === 0 && (
              <div className="p-4 text-sm text-gray-400 text-center mt-10">
                You have no active conversations.
              </div>
            )}
          </div>
        </div>

        <div className="flex-1 flex flex-col h-full bg-gray-50/50">
          {activeChatId && activeConversation ? (
            <>
              <div className="p-4 bg-white border-b border-gray-100 flex justify-between items-center shadow-sm z-10">
                <div className="flex items-center gap-3">
                  <img
                    src={activeConversation.avatar || `https://ui-avatars.com/api/?name=${activeConversation.name}&background=CCC&color=fff`}
                    alt={activeConversation.name}
                    className="w-10 h-10 rounded-full object-cover border border-gray-200"
                  />
                  <div>
                    <h3 className="font-bold text-gray-900">{activeConversation.name}</h3>
                    {onlineUsers.includes(activeConversation.id) ? (
                      <span className="text-xs text-green-600 flex items-center gap-1">
                        <span className="w-1.5 h-1.5 bg-green-600 rounded-full"></span>
                        Online
                      </span>
                    ) : (
                      <span className="text-xs text-gray-400 flex items-center gap-1">
                        <span className="w-1.5 h-1.5 bg-gray-400 rounded-full"></span>
                        Offline
                      </span>
                    )}
                  </div>
                </div>
                <div className="flex gap-2">
                  <button className="p-2 text-gray-400 hover:bg-gray-100 rounded-full">
                    <Phone size={20} />
                  </button>
                  <button className="p-2 text-gray-400 hover:bg-gray-100 rounded-full">
                    <Video size={20} />
                  </button>
                  <button className="p-2 text-gray-400 hover:bg-gray-100 rounded-full">
                    <MoreVertical size={20} />
                  </button>
                </div>
              </div>

              <div className="flex-1 overflow-y-auto p-4 space-y-4">
                {currentMessages.map((msg) => {
                  const isMe = msg.senderId === user?.id;
                  return (
                    <div
                      key={msg.id}
                      className={`flex ${isMe ? 'justify-end' : 'justify-start'}`}
                    >
                      <div
                        className={`max-w-[70%] rounded-2xl px-4 py-3 shadow-sm ${
                          isMe
                            ? 'bg-forest-600 text-white rounded-br-none'
                            : 'bg-white text-gray-800 rounded-bl-none'
                        }`}
                      >
                        <p className="text-sm">{msg.content}</p>
                        <span className={`text-[10px] mt-1 block ${isMe ? 'text-green-100' : 'text-gray-400'}`}>
                          {format(new Date(msg.timestamp), 'h:mm a')}
                        </span>
                      </div>
                    </div>
                  );
                })}
                <div ref={messagesEndRef} />
              </div>

              <div className="p-4 bg-white border-t border-gray-100">
                <form onSubmit={handleSendMessage} className="flex gap-2">
                  <input
                    type="text"
                    value={messageInput}
                    onChange={(e) => setMessageInput(e.target.value)}
                    placeholder="Type a message..."
                    className="flex-1 bg-gray-50 border-none rounded-xl px-4 py-3 focus:ring-2 focus:ring-forest-500/20"
                  />
                  <button
                    type="submit"
                    disabled={!messageInput.trim()}
                    className="bg-forest-600 text-white p-3 rounded-xl hover:bg-forest-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shadow-sm"
                  >
                    <Send size={20} />
                  </button>
                </form>
              </div>
            </>
          ) : (
            <div className="flex-1 flex items-center justify-center text-gray-400">
              Select a conversation to start chatting 
            </div>
          )}
        </div>
      </div>
    </div>
  );
INNER_EOF

# Copy it to both tourist and guide
cp /tmp/Chat.tsx /home/gihan/Documents/tour-mate/frontend/src/pages/tourist/Chat.tsx
cp /tmp/Chat.tsx /home/gihan/Documents/tour-mate/frontend/src/pages/guide/Chat.tsx

