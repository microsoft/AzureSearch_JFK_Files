import { Service } from "../service";
import { ItemCollection } from "./item.model";
import { FacetCollection } from "./facet.model";
import { FilterCollection } from "./filter.model";
import { SuggestionCollection } from "./suggestion.model";

export type ResultViewMode = "grid" | "graph";

export interface State {
  searchValue: string;
  itemCollection: ItemCollection;
  activeSearch: string;
  facetCollection: FacetCollection;
  filterCollection: FilterCollection;
  suggestionCollection: SuggestionCollection;
  resultCount: number;
  showDrawer: boolean;
  resultViewMode: ResultViewMode; 
  loading: boolean;
  pageSize: number;
  pageIndex: number;
  lastPageIndexReached: boolean;
}