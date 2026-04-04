import { useEffect, useMemo, useRef, useState } from 'react';
import type { LucideIcon } from 'lucide-react';
import {
  ArrowUpRight,
  ChevronRight,
  Info,
  MoreVertical,
  Paperclip,
  PieChart,
  Send,
  ShieldAlert,
  Sparkles,
  TrendingUp,
  Zap,
  FileText
} from 'lucide-react';
import { Badge } from '../components/Ui';
import { useAuth } from '../contexts/AuthContext';
import { agentTypeLabels, flattenErrors } from '../lib/utils';
import type { AgentChatResponse } from '../types/api';

const promptChips = [
  'How can I reduce food spending?',
  'Am I over budget this month?',
  'How should I invest my monthly surplus?',
  'Give me my monthly report summary.'
];

type ChatTone = 'indigo' | 'rose' | 'emerald' | 'amber' | 'violet';
type ActionCardType = 'anomaly' | 'investment' | 'budget' | 'report';

interface ActionCardData {
  type: ActionCardType;
  title: string;
  subtitle: string;
  body: string;
  primaryAction: string;
  secondaryAction?: string;
  tone: ChatTone;
}

interface ChatMessage {
  id: string;
  role: 'assistant' | 'user';
  content: string;
  timestamp: string;
  agentLabel?: string;
  agentIcon?: LucideIcon;
  tone?: ChatTone;
  actionCard?: ActionCardData;
}

function formatTime(value = new Date()) {
  return value.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}

function shorten(text: string, max = 200) {
  return text.length > max ? `${text.slice(0, max - 1)}…` : text;
}

function buildActionCard(agentUsed: number, prompt: string, reply: string): ActionCardData | undefined {
  const summary = shorten(reply, 200);

  if (agentUsed === 1) {
    return {
      type: 'anomaly',
      title: 'Suspicious transaction review',
      subtitle: 'Anomaly engine',
      body: summary,
      primaryAction: 'Flag for review',
      secondaryAction: 'Looks safe',
      tone: 'rose'
    };
  }

  if (agentUsed === 2) {
    return {
      type: 'budget',
      title: 'Budget adjustment idea',
      subtitle: 'Budget advisor',
      body: summary,
      primaryAction: 'Open budgets',
      secondaryAction: 'Keep monitoring',
      tone: 'amber'
    };
  }

  if (agentUsed === 4 || /invest|surplus/i.test(prompt)) {
    return {
      type: 'investment',
      title: 'Surplus allocation suggestion',
      subtitle: 'Investment advisor',
      body: summary,
      primaryAction: 'Open goals',
      secondaryAction: 'Ask for another option',
      tone: 'emerald'
    };
  }

  if (agentUsed === 5 || /report/i.test(prompt)) {
    return {
      type: 'report',
      title: 'Monthly report ready',
      subtitle: 'Report generator',
      body: summary,
      primaryAction: 'Open reports',
      secondaryAction: 'Summarize again',
      tone: 'violet'
    };
  }

  return undefined;
}

function agentPresentation(agentUsed: number) {
  switch (agentUsed) {
    case 1:
      return { label: 'Anomaly Engine', icon: ShieldAlert as LucideIcon, tone: 'rose' as const };
    case 2:
      return { label: 'Budget Advisor', icon: PieChart as LucideIcon, tone: 'amber' as const };
    case 4:
      return { label: 'Investment Advisor', icon: TrendingUp as LucideIcon, tone: 'emerald' as const };
    case 5:
      return { label: 'Report Generator', icon: FileText as LucideIcon, tone: 'violet' as const };
    default:
      return { label: 'FinPilot Core', icon: Sparkles as LucideIcon, tone: 'indigo' as const };
  }
}

function AgentBadge({ label, icon: Icon, tone = 'indigo' }: { label: string; icon?: LucideIcon; tone?: ChatTone }) {
  return (
    <div className={`coach-page__agent-badge coach-page__agent-badge--${tone}`}>
      <span className="coach-page__agent-badge-icon">{Icon ? <Icon size={12} strokeWidth={2.6} /> : <Sparkles size={12} strokeWidth={2.6} />}</span>
      <span>{label}</span>
    </div>
  );
}

function ActionCard({ data }: { data: ActionCardData }) {
  return (
    <section className={`coach-page__action-card coach-page__action-card--${data.tone}`}>
      <div className="coach-page__action-card-header">
        <div>
          <p className="coach-page__action-card-eyebrow">{data.subtitle}</p>
          <h4 className="coach-page__action-card-title">{data.title}</h4>
        </div>
        <Badge tone={data.tone === 'violet' ? 'violet' : data.tone === 'indigo' ? 'slate' : data.tone}>{data.type}</Badge>
      </div>
      <p className="coach-page__action-card-copy">{data.body}</p>
      <div className="coach-page__action-card-actions">
        <button type="button" className="coach-page__action-button coach-page__action-button--primary">{data.primaryAction}</button>
        {data.secondaryAction ? (
          <button type="button" className="coach-page__action-button coach-page__action-button--secondary">{data.secondaryAction}</button>
        ) : null}
      </div>
    </section>
  );
}

function ChatBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';

  return (
    <div className={`coach-page__message-row coach-page__message-row--${message.role}`}>
      <div className={`coach-page__message-stack coach-page__message-stack--${message.role}`}>
        <p className="coach-page__message-meta">{isUser ? 'You' : message.agentLabel ?? 'Assistant'} • {message.timestamp}</p>
        <article className={`coach-page__message coach-page__message--${message.role}`}>
          {!isUser ? <AgentBadge label={message.agentLabel ?? 'FinPilot Core'} icon={message.agentIcon} tone={message.tone} /> : null}
          <p className="coach-page__message-text">{message.content}</p>
          {!isUser && message.actionCard ? <ActionCard data={message.actionCard} /> : null}
          {!isUser && !message.actionCard ? (
            <button type="button" className="coach-page__inline-action">
              Tell me more <ChevronRight size={12} />
            </button>
          ) : null}
        </article>
      </div>
    </div>
  );
}

export function CoachPage() {
  const { api, user } = useAuth();
  const [messages, setMessages] = useState<ChatMessage[]>(() => {
    const firstName = user?.fullName?.split(' ')[0] ?? 'there';
    return [
      {
        id: 'welcome',
        role: 'assistant',
        agentLabel: 'FinPilot Core',
        agentIcon: Sparkles,
        tone: 'indigo',
        content: `Financial command initialized. Hi ${firstName}. I have your balances, recent spending, and current goals in view. Ask for clearer spending direction, budget pressure checks, anomaly review, or monthly planning.`,
        timestamp: formatTime()
      }
    ];
  });
  const [suggestions, setSuggestions] = useState<string[]>(promptChips);
  const [inputValue, setInputValue] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const riskProfile = 'moderate';
  const age = 29;

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isTyping]);

  const userTurns = useMemo(() => messages.filter((message) => message.role === 'user'), [messages]);
  const firstName = user?.fullName?.split(' ')[0] ?? 'there';
  const headerTitle = useMemo(() => `${firstName}, your AI financial desk is ready.`, [firstName]);

  const handleSend = async (text: string) => {
    const trimmed = text.trim();
    if (!trimmed) return;

    const userMessage: ChatMessage = {
      id: `${Date.now()}-user`,
      role: 'user',
      content: trimmed,
      timestamp: formatTime()
    };

    const priorUserTurns = userTurns.slice(-4).map((item) => item.content);

    setMessages((current) => [...current, userMessage]);
    setInputValue('');
    setIsTyping(true);
    setError(null);

    try {
      const response = await api.post<AgentChatResponse>('/api/agents/chat', {
        message: trimmed,
        riskProfile,
        age,
        conversationHistory: [...priorUserTurns, trimmed]
      });

      const presentation = agentPresentation(response.agentUsed);
      const assistantMessage: ChatMessage = {
        id: `${Date.now()}-assistant`,
        role: 'assistant',
        content: response.reply,
        timestamp: formatTime(new Date(response.generatedAt)),
        agentLabel: presentation.label || agentTypeLabels[response.agentUsed],
        agentIcon: presentation.icon,
        tone: presentation.tone,
        actionCard: buildActionCard(response.agentUsed, trimmed, response.reply)
      };

      setMessages((current) => [...current, assistantMessage]);
      setSuggestions(response.followUpSuggestions.length > 0 ? response.followUpSuggestions : promptChips);
    } catch (sendError) {
      setError(flattenErrors(sendError));
    } finally {
      setIsTyping(false);
    }
  };

  return (
    <div className="coach-page">
      <section className="coach-page__workspace">
        <header className="coach-page__topbar">
          <div className="coach-page__brand">
            <span className="coach-page__brand-mark"><Zap size={18} /></span>
            <div>
              <h2 className="coach-page__brand-title">FinPilot Intelligence</h2>
              <p className="coach-page__brand-status"><span className="coach-page__brand-status-dot" /> Agents online</p>
            </div>
          </div>
          <button type="button" className="coach-page__topbar-action" aria-label="More options">
            <MoreVertical size={18} />
          </button>
        </header>

        {error ? <div className="coach-page__feedback">{error}</div> : null}

        <div className="coach-page__layout">
          <main className="coach-page__chat-panel">
            <div className="coach-page__chat-header">
              <div className="coach-page__chat-header-main">
                <p className="coach-page__section-eyebrow">AI financial command</p>
                <h3 className="coach-page__chat-title">{headerTitle}</h3>
                <p className="coach-page__chat-copy">
                  Ask about spending, budgets, anomaly detection, investment direction, or monthly reporting. The coach will route to the right specialist automatically.
                </p>
              </div>
              <div className="coach-page__header-badges">
                <Badge tone="slate">{userTurns.length} messages</Badge>
                <Badge tone="emerald">Live context</Badge>
              </div>
            </div>

            <div className="coach-page__prompt-strip" aria-label="Suggested prompts">
              {suggestions.map((prompt) => (
                <button key={prompt} type="button" className="coach-page__prompt-chip" onClick={() => void handleSend(prompt)}>
                  {prompt}
                </button>
              ))}
            </div>

            <div className="coach-page__message-list">
              {messages.map((message) => (
                <ChatBubble key={message.id} message={message} />
              ))}

              {isTyping ? (
                <div className="coach-page__message-row coach-page__message-row--assistant">
                  <div className="coach-page__typing">
                    <span className="coach-page__typing-dot" />
                    <span className="coach-page__typing-dot" />
                    <span className="coach-page__typing-dot" />
                  </div>
                </div>
              ) : null}

              <div ref={messagesEndRef} />
            </div>

            <footer className="coach-page__composer-shell">
              <div className="coach-page__composer-row">
                <button type="button" className="coach-page__composer-icon" aria-label="Attach context">
                  <Paperclip size={18} />
                </button>
                <input
                  value={inputValue}
                  onChange={(event) => setInputValue(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' && !event.shiftKey) {
                      event.preventDefault();
                      void handleSend(inputValue);
                    }
                  }}
                  placeholder="Ask your AI financial command…"
                  className="coach-page__composer-input"
                />
                <button
                  type="button"
                  className={`coach-page__send-button${inputValue.trim() ? ' coach-page__send-button--ready' : ''}`}
                  onClick={() => void handleSend(inputValue)}
                  disabled={!inputValue.trim()}
                  aria-label="Send prompt"
                >
                  {inputValue.trim() ? <ArrowUpRight size={20} strokeWidth={2.4} /> : <Send size={18} strokeWidth={2.4} />}
                </button>
              </div>

              <div className="coach-page__disclaimer">
                <Info size={12} />
                <p>AI-generated guidance may be incomplete. Verify any critical financial action before acting.</p>
              </div>
            </footer>
          </main>

          <aside className="coach-page__side-column">
            <section className="coach-page__panel coach-page__panel--suggestions">
              <div className="coach-page__panel-header">
                <div>
                  <p className="coach-page__section-eyebrow">Suggested prompts</p>
                  <h4 className="coach-page__panel-title">Fast ways to begin</h4>
                </div>
                <Sparkles size={16} />
              </div>
              <div className="coach-page__suggestion-list">
                {suggestions.map((prompt) => (
                  <button key={`${prompt}-side`} type="button" className="coach-page__suggestion-card" onClick={() => void handleSend(prompt)}>
                    <span>{prompt}</span>
                    <ChevronRight size={14} />
                  </button>
                ))}
              </div>
            </section>

            <section className="coach-page__panel coach-page__panel--capabilities">
              <div className="coach-page__panel-header">
                <div>
                  <p className="coach-page__section-eyebrow">Specialists online</p>
                  <h4 className="coach-page__panel-title">What the coach can do</h4>
                </div>
                <Badge tone="slate">Auto routed</Badge>
              </div>
              <div className="coach-page__capability-list">
                <article className="coach-page__capability-card coach-page__capability-card--indigo">
                  <Sparkles size={16} />
                  <div>
                    <h5>Core advisor</h5>
                    <p>Talk through spending pressure, next actions, and monthly positioning.</p>
                  </div>
                </article>
                <article className="coach-page__capability-card coach-page__capability-card--rose">
                  <ShieldAlert size={16} />
                  <div>
                    <h5>Anomaly review</h5>
                    <p>Spot unusual transactions and frame the safest next check.</p>
                  </div>
                </article>
                <article className="coach-page__capability-card coach-page__capability-card--amber">
                  <PieChart size={16} />
                  <div>
                    <h5>Budget advisor</h5>
                    <p>Explain overspend risk and suggest a recovery move before month end.</p>
                  </div>
                </article>
                <article className="coach-page__capability-card coach-page__capability-card--emerald">
                  <TrendingUp size={16} />
                  <div>
                    <h5>Investment guide</h5>
                    <p>Translate surplus cash flow into clearer goal or allocation options.</p>
                  </div>
                </article>
              </div>
            </section>
          </aside>
        </div>
      </section>
    </div>
  );
}
