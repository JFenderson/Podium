// Band DTOs matching backend

export interface BandSummaryDto {
  id: number;
  bandName: string;
  universityName: string;
  state: string;
  city: string;
  division?: string;
  conference?: string;
  logoUrl?: string;
  memberCount?: number;
  shortDescription?: string;
}

export interface BandDetailDto {
  id: number;
  bandName: string;
  universityName: string;
  collegeWebsite?: string;
  bandWebsite?: string;
  state: string;
  city: string;
  division?: string;
  conference?: string;
  logoUrl?: string;
  description?: string;
  foundedYear?: number;
  memberCount?: number;
  directorName?: string;
  contactEmail?: string;
  phoneNumber?: string;
  isActive: boolean;
  instrumentNeeds?: InstrumentNeedDto[];
  recentAchievements?: string[];
  videoUrl?: string;
  scholarshipInfo?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface BandFilterDto {
  search?: string;
  state?: string;
  city?: string;
  division?: string;
  conference?: string;
}

export interface InstrumentNeedDto {
  instrument: string;
  currentCount: number;
  targetCount: number;
  priority: 'High' | 'Medium' | 'Low';
}

export interface InstrumentDistributionDto {
  instrument: string;
  count: number;
}

export interface BandStatsDto {
  totalMembers: number;
  instrumentDistribution: InstrumentDistributionDto[];
  averageGpa?: number;
  states: string[];
  graduationYears: number[];
}