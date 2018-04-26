export interface RectangleProps {
  id: string;
  left: number;
  top: number;
  word: string;
  isHover: boolean;
}

export const createEmptyRectangleProps = (): RectangleProps => ({
  id: '',
  isHover: false,
  left: 0,
  top: 0,
  word: '',
});
