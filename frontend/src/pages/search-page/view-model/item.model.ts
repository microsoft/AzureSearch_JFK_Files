export interface Item {
  title: string;
  subtitle?: string;
  thumbnail?: string;
  excerpt?: string;
  rating?: number;
  extraFields?: any[];
  metadata?: any;
  demoInitialPage: number;
}

export type ItemCollection = Item[];
