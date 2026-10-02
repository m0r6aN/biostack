'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { apiClient } from './api';
import { PersonProfile } from './types';
import { useOptionalAuth } from './AuthProvider';

interface ProfileContextType {
  currentProfileId: string | null;
  setCurrentProfileId: (id: string | null) => void;
  profiles: PersonProfile[];
  setProfiles: (profiles: PersonProfile[]) => void;
  isSidebarOpen: boolean;
  setSidebarOpen: (isOpen: boolean) => void;
}

const ProfileContext = createContext<ProfileContextType | undefined>(undefined);

export function ProfileProvider({ children }: { children: React.ReactNode }) {
  const [currentProfileId, setCurrentProfileId] = useState<string | null>(null);
  const [profiles, setProfiles] = useState<PersonProfile[]>([]);
  const [isHydrated, setIsHydrated] = useState(false);
  const [isSidebarOpen, setSidebarOpen] = useState(false);
  const auth = useOptionalAuth();
  const user = auth?.user ?? null;
  const loading = auth?.loading ?? false;

  // Load the user's profiles once authentication resolves so the header
  // profile picker works on every surface — previously only pages that fetched
  // profiles themselves populated the list, leaving the dropdown empty.
  useEffect(() => {
    if (loading || !user) return;
    let cancelled = false;
    apiClient
      .getProfiles()
      .then((data) => {
        if (cancelled) return;
        setProfiles(data);
        setCurrentProfileId((current) => current ?? data[0]?.id ?? null);
      })
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, [loading, user]);

  // Load from localStorage on mount
  useEffect(() => {
    const savedProfileId = localStorage.getItem('currentProfileId');
    if (savedProfileId) {
      setCurrentProfileId(savedProfileId);
    }
    setIsHydrated(true);
  }, []);

  // Save to localStorage whenever currentProfileId changes
  useEffect(() => {
    if (isHydrated) {
      if (currentProfileId) {
        localStorage.setItem('currentProfileId', currentProfileId);
      } else {
        localStorage.removeItem('currentProfileId');
      }
    }
  }, [currentProfileId, isHydrated]);

  return (
    <ProfileContext.Provider value={{ 
      currentProfileId, 
      setCurrentProfileId, 
      profiles, 
      setProfiles, 
      isSidebarOpen, 
      setSidebarOpen 
    }}>
      {children}
    </ProfileContext.Provider>
  );
}

export function useProfile() {
  const context = useContext(ProfileContext);
  if (context === undefined) {
    throw new Error('useProfile must be used within a ProfileProvider');
  }
  return context;
}
