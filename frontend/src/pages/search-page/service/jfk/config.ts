import { defaultAzPayload } from "../../../../az-api";
import { ServiceConfig } from "../../service";
import { mapStateToSuggestionPayload, mapSuggestionResponseToState } from "./mapper.suggestion";
import { mapStateToSearchPayload, mapSearchResponseToState } from "./mapper.search";

export const jfkServiceConfig: ServiceConfig = {
  serviceId: "jfk-docs",
  serviceName: "JFK Documents",
  serviceIcon: "fingerprint",
  
  searchConfig: {
    apiConfig: {
      protocol: process.env.SEARCH_CONFIG_PROTOCOL,
      serviceName: process.env.SEARCH_CONFIG_SERVICE_NAME,
      serviceDomain: process.env.SEARCH_CONFIG_SERVICE_DOMAIN,
      servicePath: process.env.SEARCH_CONFIG_SERVICE_PATH,
      apiVer: process.env.SEARCH_CONFIG_API_VER,
      apiKey: process.env.SEARCH_CONFIG_API_KEY,
      method: "GET",
    },
    defaultPayload: defaultAzPayload,
    mapStateToPayload: mapStateToSearchPayload,
    mapResponseToState: mapSearchResponseToState,
  },

  suggestionConfig: {
    apiConfig: {
      protocol: process.env.SUGGESTION_CONFIG_PROTOCOL,
      serviceName: process.env.SUGGESTION_CONFIG_SERVICE_NAME,
      serviceDomain: process.env.SUGGESTION_CONFIG_SERVICE_DOMAIN,
      servicePath: process.env.SUGGESTION_CONFIG_SERVICE_PATH,
      apiVer: process.env.SUGGESTION_CONFIG_API_VER,
      apiKey: process.env.SUGGESTION_CONFIG_API_KEY,
      method: "GET",
    },
    defaultPayload: {
      ...defaultAzPayload,
      count: false,
      top: 15,
      suggesterName: "sg-jfk",
      //autocompleteMode: "twoTerms",
    },
    mapStateToPayload: mapStateToSuggestionPayload,
    mapResponseToState: mapSuggestionResponseToState,
  },

  initialState: {
    facetCollection: [
      {
        fieldId: "tags",
        displayName: "Tags",
        iconName: null,
        selectionControl: "checkboxList",
        maxCount: 10,
        values: null,
      },
    ]
  }  
}
