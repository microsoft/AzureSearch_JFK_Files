import { synonyms } from "../../common/constants/synonyms";

// Proof of concept valid for demo to get highlight synonyms.
export const buildTargetWords = (activeSearch: string) => {
  const activeTargetWords = getActiveSearchTargetWords(activeSearch);
  const synonymTargetWords = getSynonymTargetWords(activeTargetWords);

  const targetWords = [
    ...activeTargetWords,
    ...synonymTargetWords,
  ];
  
  return getUniqueWords(targetWords);
};

const getActiveSearchTargetWords = (activeSearch: string) => (
  Boolean(activeSearch) ?
    activeSearch.split(" ") :
    []
);

const getSynonymTargetWords = (currentTargetWords: string[]) => (
  synonyms.reduce((total, synonymList) => ([
    ...total,
    ...getSynonyms(currentTargetWords, synonymList),
  ]), [])
);

const getSynonyms = (targetWords: string[], synonyms: string[]) => {
  return haveSomeSynonym(targetWords, synonyms) ?
    synonyms :
    []
};

const haveSomeSynonym = (targetWords: string[], synonymList: string[]) => (
  synonymList.some((synonym) => haveSynonym(targetWords, synonym))
);

const haveSynonym = (targetWords: string[], synonym: string) => (
  targetWords.some((targetWord) => synonym === targetWord)
);

const getUniqueWords = (array: string[]) => ([
  ...new Set(array),
]);
