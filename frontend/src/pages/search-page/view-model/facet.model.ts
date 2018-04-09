export interface FacetValue {
  value: string;
  count: number;
}

export interface Facet {
  fieldId: string;
  displayName: string;
  iconName?: string;
  selectionControl: string;
  values: FacetValue[];
  maxCount: number;
}

export type FacetCollection = Facet[];
