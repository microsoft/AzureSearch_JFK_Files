import * as React from "react";
import { Tooltip } from "material-ui";
import { cryptonyms } from "../../../constants/cryptonyms";
import { RectangleProps } from "./rectangleProps";

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
  }

  updateTooltip = (rectangleProps: RectangleProps) => {
    this.setState({
      isOpen: rectangleProps.isHover,
      left: rectangleProps.left,
      top: rectangleProps.top,
      message: getTooltipMessage(rectangleProps.word),
    });
  }

  render() {
    return (
      <>
        {
          this.state.isOpen &&
          Boolean(this.state.message) &&
          <Tooltip
            title={this.state.message}
            open={true}
            placement="top"
            style={{
              position: 'absolute',
              top: this.state.top,
              left: this.state.left,
            }}
          >
            <p style={{ visibility: 'hidden', border: '1px solid red' }}>test</p>
          </Tooltip>
        }
      </>
    );
  }
}

const getTooltipMessage = (word: string): string => {
  const regex = new RegExp(word, 'i');
  const cryptonym = Object.keys(cryptonyms).find((key) => regex.test(key));

  return Boolean(cryptonym) ?
    cryptonyms[cryptonym] :
    '';
}
