import { isArrayEmpty } from "../../../../util";
import { ServiceConfig } from "../../service";
import { 
  AzResponse,
  AzResponseFacet,
  AzPayload,
  AzPayloadFacet,
  AzFilterGroup,
  AzFilterCollection
} from "../../../../az-api";
import {
  Item,
  ItemCollection,
  FacetCollection,
  FacetValue,
  Facet,
  State,
  FilterCollection,
  Filter,
} from "../../view-model";

// [Search] FROM AzApi response TO view model.

const mapImgUrlInMetadata = (metadata: string) => {
  const captures = /title=(?:'|")image\s?"(.+)"/g.exec(metadata);
  return captures && captures.length ? captures[1] : "";
};

const mapResultToItem = (result: any): Item => {
  return result ? {
    title: result.id,
    subtitle: "",
    thumbnail: mapImgUrlInMetadata(result.metadata),
    excerpt: "",
    rating: 0,
    extraFields: [result.tags],
    metadata: result.metadata,
  } : null;
};

const mapSearchResponseForResults = (response: AzResponse): ItemCollection => {
  return isArrayEmpty(response.value) ? null : response.value.map(r => mapResultToItem(r));
};

const mapResponseFacetToViewFacet = (responseFacet: AzResponseFacet, baseFacet: Facet): Facet => {
  return responseFacet ? ({
    ...baseFacet,
    values: responseFacet.values.map(responseFacetValue => ({
      value: responseFacetValue.value,
      count: responseFacetValue.count,
    } as FacetValue)),
  }) : null;
};

const mapSearchResponseForFacets = (response: AzResponse, baseFacets: FacetCollection): FacetCollection => {
  return isArrayEmpty(response.facets) ? null :
    baseFacets.map(bf => 
      mapResponseFacetToViewFacet(response.facets.find(rf => rf.fieldName === bf.fieldId), bf)
    ).filter(f => f && !isArrayEmpty(f.values));
};

export const mapSearchResponseToState = (state: State, response: AzResponse, config: ServiceConfig): State => {
  const viewFacets = isArrayEmpty(state.facetCollection) ? config.initialState.facetCollection : 
  state.facetCollection; 
  return {
    ...state,
    resultCount: response.count,
    itemCollection: mapSearchResponseForResults(response),
    facetCollection: mapSearchResponseForFacets(response, viewFacets),
  }
};


// [Search] FROM view model TO AzApi.

const mapViewFilterToPayloadCollectionFilter = (filter: Filter): AzFilterCollection => {
  return filter ? {
    fieldName: filter.fieldId,
    mode: "any",
    operator: "eq",
    value: filter.store,
  } : null;
}

// TODO: WARNING, this is just tailor made for JFK single tag facet.
const mapViewFiltersToPayloadFilters = (filters: FilterCollection): AzFilterGroup => {
  if (isArrayEmpty(filters)) return null;
  // TODO: Only collection filter implemented.
  const filterGroup: AzFilterGroup = {
    logic: "and",
    items: filters.map(f => mapViewFilterToPayloadCollectionFilter(f)).filter(f => f),
  };
  return filterGroup;
};

const mapViewFacetToPayloadFacet = (viewFacet: Facet): AzPayloadFacet => {
  return {
    fieldName: viewFacet.fieldId,
    config: {
      count: viewFacet.maxCount,
    },
  };
};

export const mapStateToSearchPayload = (state: State, config: ServiceConfig): AzPayload => {
  const viewFacets = isArrayEmpty(state.facetCollection) ? config.initialState.facetCollection : 
    state.facetCollection;
  return {
    ...config.searchConfig.defaultPayload,
    search: state.searchValue,
    top: state.pageSize,
    skip: state.pageIndex * state.pageSize,
    facets: viewFacets.map(f => mapViewFacetToPayloadFacet(f)),
    filters: mapViewFiltersToPayloadFilters(state.filterCollection),
  };
}


