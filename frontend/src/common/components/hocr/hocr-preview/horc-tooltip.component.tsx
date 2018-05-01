import * as React from "react";
import { Tooltip } from "material-ui";
import { cryptonyms } from "../../../constants/cryptonyms";
import { RectangleProps } from "./rectangleProps";
import { TooltipComponent } from "../tooltip";

const topOffset = 15;

interface Props {
  rectangleProps: RectangleProps;
}

interface State {
  isOpen: boolean;
  left: number;
  top: number;
  message: string;
}

export class HocrTooltipComponent extends React.PureComponent<Props, State> {
  state = {
    isOpen: false,
    left: 0,
    top: 0,
    message: '',
  }

  componentWillReceiveProps({ rectangleProps }: Props) {
    if (rectangleProps.isHover !== this.props.rectangleProps.isHover) {
      this.updateTooltip(rectangleProps);
    }

    document.body.style.overflow = getBodyOverflow(rectangleProps.isHover);
  }

  updateTooltip = (rectangleProps: RectangleProps) => {
    this.setState({
      isOpen: rectangleProps.isHover,
      left: rectangleProps.left,
      top: rectangleProps.top + rectangleProps.height + topOffset,
      message: getTooltipMessage(rectangleProps.word),
    });
  }

  render() {
    return (
      <TooltipComponent
        show={this.state.isOpen && Boolean(this.state.message)}
        top={this.state.top}
        left={this.state.left}
      >
        {this.state.message}
      </TooltipComponent>
    );
  }
}

const getTooltipMessage = (word: string): string => {
  const cryptonym = Object.keys(cryptonyms).find((key) => (
    Boolean(word) &&
    key.toLowerCase() === word.toLowerCase()
  ));

  return Boolean(cryptonym) ?
    cryptonyms[cryptonym] :
    '';
};

const getBodyOverflow = (isHover: boolean) => (
  isHover ?
    'hidden' :
    ''
);
