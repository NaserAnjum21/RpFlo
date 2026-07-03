import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

const BDT = 'Asia/Dhaka';

function ordinal(day: number): string {
  if (day > 3 && day < 21) return day + 'th';
  switch (day % 10) {
    case 1: return day + 'st';
    case 2: return day + 'nd';
    case 3: return day + 'rd';
    default: return day + 'th';
  }
}

export function formatDate(iso: string): string {
  const d = new Date(iso);
  const day = d.toLocaleDateString('en-US', { timeZone: BDT, day: 'numeric' });
  const month = d.toLocaleDateString('en-US', { timeZone: BDT, month: 'long' });
  const year = d.toLocaleDateString('en-US', { timeZone: BDT, year: 'numeric' });
  return `${ordinal(parseInt(day))} ${month}, ${year}`;
}

export function formatDateTime(iso: string): string {
  const d = new Date(iso);
  const date = formatDate(iso);
  const time = d.toLocaleTimeString('en-US', {
    timeZone: BDT,
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });
  return `${date} at ${time}`;
}
