import { synonyms } from "../../common/constants/synonyms";
import { getUniqueStrings } from "../../util";

// Proof of concept valid for demo to get highlight synonyms.
export const buildTargetWords = (activeSearch: string) => {
  const activeTargetWords = getActiveSearchTargetWords(activeSearch);

  const targetWords = [
    ...activeTargetWords,
  ];
  
  return getUniqueStrings(targetWords);
};

const getActiveSearchTargetWords = (activeSearch: string) => (
  Boolean(activeSearch) ?
    activeSearch.split(" ") :
    []
);
