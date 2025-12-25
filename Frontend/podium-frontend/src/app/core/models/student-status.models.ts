export enum StudentAccountStatus {
  Active = 'Active',
  Pending = 'Pending',
  Suspended = 'Suspended',
  AwaitingGuardian = 'AwaitingGuardian',
  Incomplete = 'Incomplete'
}

export interface StudentStatusConfig {
  status: StudentAccountStatus | string;
  isMinor: boolean;
  guardianLinked: boolean;
}