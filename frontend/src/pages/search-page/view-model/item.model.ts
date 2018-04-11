export interface Item {
  title: string;
  subtitle?: string;
  thumbnail?: string;
  excerpt?: string;
  rating?: number;
  extraFields?: any[];
  metadata?: any;
}

export type ItemCollection = Item[];
