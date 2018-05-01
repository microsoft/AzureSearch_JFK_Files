import * as React from "react";
import { ItemComponent } from "./item.component";
import { ItemCollection, Item } from "../../view-model";
import { cnc } from "../../../../util";

const style = require("./item-collection-view.style.scss");


interface ItemViewProps {
  items?: ItemCollection;
  listMode?: boolean;
  activeSearch?: string;
  targetWords: string[];
  onClick?: (item: Item) => void;
}

export class ItemCollectionViewComponent extends React.Component<ItemViewProps, {}> {
  public constructor(props) {
    super(props);
  }
  
  public render() {
    return (    
      <div className={cnc(style.container, this.props.listMode && style.containerList)}>
        { this.props.items ? 
          this.props.items.map((child, index) => (
            <ItemComponent
              item={child}
              listMode={this.props.listMode}
              activeSearch={this.props.activeSearch}
              targetWords={this.props.targetWords}
              onClick={this.props.onClick}
              key={index}
            />
          ))
        : null }
      </div>
    );
  }  
}
