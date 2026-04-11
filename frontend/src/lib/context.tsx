'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { PersonProfile } from './types';
import { useApiAuth } from './useApiAuth';

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

  // Keep ApiClient's Bearer token synced with the NextAuth session
  useApiAuth();

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
    if (isHydrated && currentProfileId) {
      localStorage.setItem('currentProfileId', currentProfileId);
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
