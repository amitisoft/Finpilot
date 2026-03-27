import { useEffect, useRef, useState } from 'react';
import { Bot, CornerDownRight, Send, Sparkles } from 'lucide-react';
import { Badge, GlassCard } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { agentTypeLabels, flattenErrors } from '../lib/utils';
import type { AgentChatResponse } from '../types/api';

const promptChips = [
  'How can I improve my food spending?',
  'Am I over budget this month?',
  'Does this transaction look suspicious?',
  'How should I invest my monthly surplus?',
  'Give me my monthly report summary'
];

interface ChatMessage {
  role: 'assistant' | 'user';
  content: string;
  meta?: string;
}

export function CoachPage() {
  const { api } = useAuth();
  const [messages, setMessages] = useState<ChatMessage[]>([
    { role: 'assistant', content: 'Welcome to FinPilot Advisor. Ask about spending, budgets, anomalies, reports, or monthly surplus planning.', meta: 'Advisor' }
  ]);
  const [suggestions, setSuggestions] = useState<string[]>(promptChips);
  const [message, setMessage] = useState('');
  const [riskProfile] = useState('moderate');
  const [age] = useState(29);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const messageListRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const list = messageListRef.current;
    if (!list) return;
    list.scrollTo({ top: list.scrollHeight, behavior: 'smooth' });
  }, [messages]);

  const sendChat = async (text: string) => {
    if (!text.trim()) return;

    setSending(true);
    setError(null);

    const nextHistory = [...messages.filter((item) => item.role === 'user').slice(-4).map((item) => item.content), text];
    setMessages((current) => [...current, { role: 'user', content: text }]);

    try {
      const response = await api.post<AgentChatResponse>('/api/agents/chat', {
        message: text,
        riskProfile,
        age,
        conversationHistory: nextHistory
      });

      setMessages((current) => [...current, { role: 'assistant', content: response.reply, meta: agentTypeLabels[response.agentUsed] }]);
      setSuggestions(response.followUpSuggestions.length > 0 ? Array.from(response.followUpSuggestions) : promptChips);
      setMessage('');
    } catch (sendError) {
      setError(flattenErrors(sendError));
    } finally {
      setSending(false);
    }
  };

  const handleComposerKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      void sendChat(message);
    }
  };

  return (
    <div className="flex h-full min-h-0 flex-col gap-4 overflow-hidden">
      <div className="flex shrink-0 items-start justify-between gap-4">
        <div>
          <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">AI Coach</p>
          <h3 className="mt-2 text-3xl font-black tracking-tight text-slate-900">A focused finance chat workspace.</h3>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-500">Ask naturally. FinPilot resolves the right financial context behind the scenes so the workspace stays focused on conversation.</p>
        </div>
        <Badge tone="violet">Advisor online</Badge>
      </div>

      {error && <GlassCard className="shrink-0 border-rose-200 bg-rose-50 text-rose-700">{error}</GlassCard>}

      <div className="grid min-h-0 flex-1 gap-4 xl:grid-cols-[minmax(0,1.8fr)_320px]">
        <GlassCard className="min-h-0 min-w-0 overflow-hidden p-0">
          <div className="flex h-full min-h-0 flex-col">
            <div className="shrink-0 border-b border-slate-200 px-5 py-4">
              <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                <div>
                  <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Conversation</p>
                  <h4 className="mt-2 text-2xl font-black text-slate-900">Financial coach and specialist routing</h4>
                </div>
                <div className="flex items-center gap-2">
                  <Badge tone="slate">{Math.max(messages.length - 1, 0)} messages</Badge>
                  <Badge tone="emerald">Live coach</Badge>
                </div>
              </div>
            </div>

            <div ref={messageListRef} className="min-h-0 flex-1 space-y-4 overflow-y-auto bg-slate-50/80 px-4 py-5 md:px-5">
              {messages.map((item, index) => (
                <div key={`${item.role}-${index}`} className={`flex ${item.role === 'assistant' ? 'justify-start' : 'justify-end'}`}>
                  <div className={`max-w-[82%] rounded-[1.5rem] px-4 py-3 shadow-sm ${item.role === 'assistant' ? 'border border-slate-200 bg-white text-slate-900' : 'bg-gradient-to-r from-indigo-500 via-violet-500 to-fuchsia-500 text-white'}`}>
                    {item.role === 'assistant' && (
                      <div className="mb-2 flex items-center gap-2 text-[11px] font-black uppercase tracking-[0.3em] text-slate-400">
                        <Bot className="h-3.5 w-3.5" />
                        {item.meta ?? 'Advisor'}
                      </div>
                    )}
                    <p className="text-sm leading-7">{item.content}</p>
                  </div>
                </div>
              ))}
            </div>

            <div className="shrink-0 border-t border-slate-200 bg-white p-4 md:p-5">
              <div className="flex gap-3">
                <input
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  onKeyDown={handleComposerKeyDown}
                  placeholder="Ask your AI financial coach…"
                  className="min-w-0 flex-1 rounded-[1.5rem] border border-slate-200 bg-white px-5 py-4 text-sm text-slate-900"
                />
                <button onClick={() => void sendChat(message)} disabled={sending || !message.trim()} className="inline-flex shrink-0 items-center justify-center gap-2 rounded-[1.5rem] bg-slate-900 px-5 py-4 text-sm font-black uppercase tracking-[0.3em] text-white disabled:cursor-not-allowed disabled:opacity-60"><Send className="h-4 w-4" /> Send</button>
              </div>
            </div>
          </div>
        </GlassCard>

        <div className="min-h-0 min-w-0 space-y-4 xl:flex xl:flex-col xl:gap-4 xl:space-y-0">
          <GlassCard>
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">Suggested prompts</p>
                <h4 className="mt-2 text-xl font-black text-slate-900">Message ideas</h4>
              </div>
              <Sparkles className="h-5 w-5 text-violet-500" />
            </div>
            <div className="mt-4 space-y-3">
              {suggestions.map((chip) => (
                <button key={chip} onClick={() => void sendChat(chip)} disabled={sending} className="flex w-full items-center justify-between rounded-3xl border border-slate-200 bg-slate-50 px-4 py-3 text-left text-sm text-slate-700 transition hover:border-slate-300 hover:bg-white disabled:cursor-not-allowed disabled:opacity-60">
                  <span>{chip}</span>
                  <CornerDownRight className="h-4 w-4 text-slate-500" />
                </button>
              ))}
            </div>
          </GlassCard>

          <GlassCard className="shrink-0">
            <div className="flex items-center justify-between gap-3">
              <div>
                <p className="text-xs font-black uppercase tracking-[0.35em] text-slate-400">What the coach can do</p>
                <h4 className="mt-2 text-xl font-black text-slate-900">Built-in specialties</h4>
              </div>
              <Badge tone="slate">Auto routed</Badge>
            </div>
            <div className="mt-4 space-y-3">
              {[
                'Spending guidance and category reduction ideas',
                'Budget risk checks and safe-to-spend help',
                'Suspicious transaction explanations',
                'Monthly report and surplus planning support'
              ].map((item) => (
                <div key={item} className="rounded-3xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm leading-6 text-slate-600">
                  {item}
                </div>
              ))}
            </div>
          </GlassCard>
        </div>
      </div>
    </div>
  );
}
