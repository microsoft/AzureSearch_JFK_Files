'use strict';

var Util = {
    onReady: function (callback) {
        if (document.readyState != 'loading') callback();
        else document.addEventListener('DOMContentLoaded', callback);
    },

    get: function (url, callback) {
        var request = new XMLHttpRequest();
        request.open('GET', url);
        request.onload = function () {
            if (request.status >= 200 && request.status < 400) {
                callback(null, request.responseText);
            } else {
                callback(new Error('Error loading url "' + url + '": HTTP error: ' + request.status + ' ' + request.statusText));
            }
        };
        request.onerror = function () {
            callback(new Error('Error loading url "' + url + '": HTTP connection error'));
        };
        request.send();
    },

    handleError: function (err) {
        alert(err.message); // TODO
    },

    createElem: function (name, attributes) {
        var node = document.createElement(name);
        for (var name in attributes) {
            node.setAttribute(name, attributes[name]);
        }
        return node;
    },

    createSvgElem: function (name, attributes) {
        var node = document.createElementNS('http://www.w3.org/2000/svg', name);
        for (var name in attributes) {
            node.setAttribute(name, attributes[name]);
        }
        return node;
    },

    removeChildren: function (node) {
        while (node.hasChildNodes()) {
            node.removeChild(node.lastChild);
        }
    }
};


function HocrProofreader(config) {
    this.config = config;

    this.layoutSvg = Util.createSvgElem('svg', {'class': 'layout'});

    this.layoutBackground = Util.createSvgElem('rect', {'class': 'background', 'x': 0, 'y': 0, 'width': '100%', 'height': '100%', 'style': 'fill: none'});
    this.layoutSvg.appendChild(this.layoutBackground);

    this.layoutImage = Util.createSvgElem('image', {'x': 0, 'y': 0, 'width': '100%', 'height': '100%'});
    this.layoutSvg.appendChild(this.layoutImage);

    this.layoutWords = Util.createSvgElem('g', {'class': 'words'});
    this.layoutSvg.appendChild(this.layoutWords);

    this.layoutRects = Util.createSvgElem('g', {'class': 'rects'});
    this.layoutSvg.appendChild(this.layoutRects);

    this.layoutContainer = document.getElementById(config.layoutContainer);
    this.layoutContainer.appendChild(this.layoutSvg);
    this.layoutContainer.style.overflow = 'scroll';

    this.editorIframe = Util.createElem('iframe', {'class': 'editor', 'frameborder': 0});

    var editorContainer = document.getElementById(config.editorContainer);
    editorContainer.appendChild(this.editorIframe);

    var self = this;
    self.hoveredNode = null;
    self.mousePosition = null;

    this.layoutSvg.addEventListener('mousemove', function (event) {
        self.mousePosition = {container: 'layout', x: event.clientX, y: event.clientY};
        self.onHover(event.target);
    });
    this.layoutSvg.addEventListener('mouseleave', function (event) {
        self.mousePosition = null;
        self.onHover(null);
    });
    this.layoutContainer.addEventListener('scroll', function (event) {
        if (!self.mousePosition || self.mousePosition.container !== 'layout') return;
        self.onHover(document.elementFromPoint(self.mousePosition.x, self.mousePosition.y));
    });

    // init some defaults:
    this.currentPage = null;
    this.toggleLayoutImage();
    this.setZoom('page-width');
}

HocrProofreader.prototype.setHocr = function (hocr, baseUrl, startPage, highlightWords) {
    this.highlightWords = highlightWords;
    this.hocrBaseUrl = baseUrl;
    var hocrDoc = this.editorIframe.contentDocument;

    // TODO: use baseUrl for images/components in hOCR - use <base>?

    hocrDoc.open();
    hocrDoc.write(hocr);
    hocrDoc.close();

    var self = this;
    var hocrRoot = hocrDoc.documentElement;
    hocrRoot.addEventListener('mousemove', function (event) {
        self.mousePosition = {container: 'editor', x: event.clientX, y: event.clientY};
        self.onHover(event.target, true);
    });
    hocrRoot.addEventListener('mouseleave', function (event) {
        self.mousePosition = null;
        self.onHover(null, true);
    });
    hocrDoc.addEventListener('scroll', function (event) {
        if (!self.mousePosition || self.mousePosition.container !== 'editor') return;
        self.onHover(hocrDoc.elementFromPoint(self.mousePosition.x, self.mousePosition.y), true);
    });

    this.editorStylesheet = Util.createElem('link', {'type': 'text/css', 'rel': 'stylesheet', 'href': 'editor.css'});
    hocrDoc.head.appendChild(this.editorStylesheet);

    hocrDoc.body.contentEditable = true;

    this.setPage(startPage || 'first');

    // scroll the page into view if we start on a different page
    // note this isnt working in a modal when not visible
    this.editorIframe.contentDocument.body.children[startPage].scrollIntoView({ behavior: "instant", block: "start" });
};

HocrProofreader.prototype.getHocr = function () {
    var hocrDoc = this.editorIframe.contentDocument;

    hocrDoc.head.removeChild(this.editorStylesheet);
    hocrDoc.body.contentEditable = 'inherit'; // this removes the attribute from DOM
    this.onHover(null); // ensure there are no "hover" classes left

    var serializer = new XMLSerializer();
    var hocr = serializer.serializeToString(hocrDoc);

    hocrDoc.head.appendChild(this.editorStylesheet);
    hocrDoc.body.contentEditable = true;

    return hocr;
};

HocrProofreader.prototype.setZoom = function (zoom) {
    if (zoom) this.currentZoom = zoom;

    if (this.currentZoom === 'page-full') {
        this.layoutSvg.style.width = null;
        this.layoutSvg.style.height = null;
        this.layoutSvg.style.maxWidth = '100%';
        this.layoutSvg.style.maxHeight = '100%';
    } else if (this.currentZoom === 'page-width') {
        this.layoutSvg.style.width = null;
        this.layoutSvg.style.height = null;
        this.layoutSvg.style.maxWidth = '100%';
        this.layoutSvg.style.maxHeight = null;
    } else if (this.currentZoom === 'original') {
        if (this.currentPage) {
            var options = this.getNodeOptions(this.currentPage);
            this.layoutSvg.style.width = '' + (options.bbox[2] - options.bbox[0]) + 'px';
            this.layoutSvg.style.height = '' + (options.bbox[3] - options.bbox[1]) + 'px';
        } else {
            this.layoutSvg.style.width = null;
            this.layoutSvg.style.height = null;
        }

        this.layoutSvg.style.maxWidth = null;
        this.layoutSvg.style.maxHeight = null;
    }
};

HocrProofreader.prototype.toggleLayoutImage = function () {
    if (!this.layoutWords.style.display || this.layoutWords.style.display === 'block') {
        this.layoutWords.style.display = 'none';
        this.layoutImage.style.display = 'block';
    } else {
        this.layoutWords.style.display = 'block';
        this.layoutImage.style.display = 'none';
    }
};

HocrProofreader.prototype.setPage = function (page) {
    var pageNode, backwards = false, skipCurrent = false;
    var hocrDoc = this.editorIframe.contentDocument;

    if (page === 'first') {
        pageNode = hocrDoc.body.firstElementChild;
    } else if (page === 'last') {
        pageNode = hocrDoc.body.lastElementChild;
        backwards = true;
    } else if (page === 'next') {
        pageNode = this.currentPage || hocrDoc.body.firstElementChild;
        skipCurrent = true;
    } else if (page === 'previous') {
        pageNode = this.currentPage || hocrDoc.body.lastElementChild;
        backwards = true;
        skipCurrent = true;
    }
    else // assume it is a number
    {
        pageNode = hocrProofreader.editorIframe.contentDocument.body.children[page];
    }


    while (pageNode && (skipCurrent || !pageNode.classList.contains('ocr_page'))) {
        pageNode = backwards ? pageNode.previousElementSibling : pageNode.nextElementSibling;
        skipCurrent = false;
    }

    this.renderPage(pageNode || null);
};

HocrProofreader.prototype.renderPage = function (pageNode) {
    this.layoutContainer.scrollTop = 0;
    this.layoutContainer.scrollLeft = 0;

    var scrollToBottom = false, tmpNode = this.currentPage;
    while (tmpNode) {
        tmpNode = tmpNode.previousElementSibling;
        if (tmpNode === pageNode) {
            scrollToBottom = true;
            break;
        }
    }

    function removeLinkedNodes(node) {
        if (node.linkedNode) node.linkedNode = null;

        var childNode = node.firstElementChild;
        while (childNode) {
            removeLinkedNodes(childNode);
            childNode = childNode.nextElementSibling;
        }
    }
    if (this.currentPage) removeLinkedNodes(this.currentPage);

    Util.removeChildren(this.layoutWords);
    Util.removeChildren(this.layoutRects);

    this.currentPage = pageNode;

    this.setZoom();
    this.layoutImage.removeAttribute('transform');

    if (!this.currentPage) {
        // TODO: hide completely? reset image/font/viewBox/...?
        return;
    }

    var pageOptions = this.getNodeOptions(this.currentPage);

    this.layoutSvg.setAttribute('viewBox', pageOptions.bbox.join(' '));
    this.layoutWords.style.fontFamily = 'Liberation Serif, serif'; // TODO: use font from hOCR (per page)

    this.layoutImage.setAttributeNS('http://www.w3.org/1999/xlink', 'href', pageOptions.image.substring(0, 4).toLowerCase() == 'http' ? pageOptions.image : this.hocrBaseUrl + pageOptions.image);

    if (pageOptions.textangle) {
        // textangle is counter-clockwise, so we have to rotate the image clockwise - and transform-rotate() is clockwise:
        this.layoutImage.setAttribute('transform', 'rotate(' + pageOptions.textangle + ' ' +
            ((pageOptions.bbox[2] - pageOptions.bbox[0]) / 2) + ' ' +
            ((pageOptions.bbox[3] - pageOptions.bbox[1]) / 2) + ')');
    }

    this.renderNodesRecursive(this.currentPage, pageOptions);

    if (scrollToBottom) {
        this.layoutContainer.scrollTop = this.layoutContainer.scrollHeight - this.layoutContainer.clientHeight;
    }
};

HocrProofreader.prototype.renderNodesRecursive = function (node, options, parentRectsNode) {
    if (!parentRectsNode) parentRectsNode = this.layoutRects;

    var className = null;
    if (node.classList.contains('ocr_carea')) {
        className = 'ocr_carea';
    } else if (node.classList.contains('ocr_par')) {
        className = 'ocr_par';
    } else if (node.classList.contains('ocr_line')) {
        className = 'ocr_line';
    } else if (node.classList.contains('ocrx_word')) {
        className = 'ocrx_word';
    }

    if (className) {
        if (className !== 'ocrx_word') {
            var groupNode = Util.createSvgElem('g', {'class': className});
            parentRectsNode.appendChild(groupNode);
            parentRectsNode = groupNode;
        }

        options = this.inheritOptions(this.getNodeOptions(node), options);

        if (options.bbox) {
            if (className === 'ocrx_word' && options.baselineBbox) {
                var word = node.textContent;

                // TODO: calculate font-size and y based on bbox, not baseline (font-metrics needed):
                var textNode = Util.createSvgElem('text', {
                    'x': options.bbox[0],
                    'y': parseFloat(options.baselineBbox[3]) + parseFloat(options.baseline[1]),
                    'font-size': 14,//options.x_fsize * options.scan_res[1] / 72, // 1 pt = 1/72 inch
                    'textLength': options.bbox[2] - options.bbox[0],
                    'lengthAdjust': 'spacingAndGlyphs'
                });
                textNode.textContent = word;
                this.layoutWords.appendChild(textNode);
            }

            var rectNode = Util.createSvgElem('rect', {
                'x': options.bbox[0],
                'y': options.bbox[1],
                'width': options.bbox[2] - options.bbox[0],
                'height': options.bbox[3] - options.bbox[1],
                'class': className
            });

            if (className === 'ocrx_word' && $.inArray(node.innerText.toLowerCase(), this.highlightWords) >= 0) {
                $(node).addClass("highlight");
                $(rectNode).addClass("highlight");
            }

            parentRectsNode.appendChild(rectNode);

            // cross-link both nodes:
            rectNode.linkedNode = node;
            node.linkedNode = rectNode;
        }
    }

    var childNode = node.firstElementChild;
    while (childNode) {
        this.renderNodesRecursive(childNode, options, parentRectsNode);
        childNode = childNode.nextElementSibling;
    }
};

HocrProofreader.prototype.getNodeOptions = function (node) {
    var asArray = ['bbox', 'baseline', 'scan_res'];
    var optionsStr = node.title ? node.title : '';
    var match, regex = /(?:^|;)\s*(\w+)\s+(?:([^;"']+?)|"((?:\\"|[^"])+?)"|'((?:\\'|[^'])+?)')\s*(?=;|$)/g;

    var options = {};
    while (match = regex.exec(optionsStr)) {
        var name = match[1];
        var value = match[4] || match[3] || match[2];

        if (asArray.indexOf(name) !== -1) {
            value = value.split(/\s+/);
        }

        options[name] = value;
    }

    return options;
};

HocrProofreader.prototype.inheritOptions = function (options, parentOptions) {
    var inheritableOptions = ['baseline', 'baselineBbox', 'x_fsize', 'scan_res'];

    // baseline is relative to the bbox of the node where the baseline is defined, so we have to remember this bbox:
    if ('baseline' in options && 'bbox' in options) {
        options.baselineBbox = options.bbox;
    }

    if (parentOptions) {
        for (var name in parentOptions) {
            if (inheritableOptions.indexOf(name) === -1) continue;
            if (name in options) continue;
            options[name] = parentOptions[name];
        }
    }

    return options;
};

HocrProofreader.prototype.onHover = function (target, isEditorContainer) {
    if (target === this.hoveredNode) return;

    if (this.hoveredNode) {
        this.hoverTreeNodes(this.hoveredNode, false);
        this.hoverTreeNodes(this.hoveredNode.linkedNode, false);
        this.hoveredNode = null;
    }

    if (isEditorContainer) {
        // check for page change:
        var pageNode = target;
        while (pageNode && (!pageNode.classList.contains('ocr_page'))) {
            pageNode = pageNode.parentElement;
        }
        if (pageNode && pageNode !== this.currentPage) {
            this.renderPage(pageNode);
        }
    }

    var linkedNode = target && target.linkedNode;
    if (linkedNode) {
        this.hoverTreeNodes(target, true);
        this.hoverTreeNodes(linkedNode, true);
        this.hoveredNode = target;

        var linkedContainer = isEditorContainer ? this.layoutContainer : this.editorIframe.contentDocument.documentElement;
        this.scrollIntoViewIfNeeded(linkedNode, linkedContainer);
    }
};

HocrProofreader.prototype.hoverTreeNodes = function (node, isActive) {
    while (node) {
        if (node.classList.contains('ocr_page') || node.classList.contains('rects')) break;
        if (isActive) {
            node.classList.add('hover');
        } else {
            node.classList.remove('hover');
        }
        node = node.parentElement;
    }
};

HocrProofreader.prototype.scrollIntoViewIfNeeded = function (node, scrollParentNode) {
    var rect = node.getBoundingClientRect();
    // do not substract the bounding-rect of the scrollParent if it is the documentElement (e.g. the iframe),
    // otherwise scroll-position is added twice - set to 0:
    var parentRect = scrollParentNode.parentElement ? scrollParentNode.getBoundingClientRect() : {left: 0, top: 0};
    var nodeRect = {
        left: rect.left - parentRect.left + scrollParentNode.scrollLeft,
        top: rect.top - parentRect.top + scrollParentNode.scrollTop,
        right: rect.right - parentRect.left + scrollParentNode.scrollLeft,
        bottom: rect.bottom - parentRect.top + scrollParentNode.scrollTop
    };

    if (nodeRect.bottom - nodeRect.top <= scrollParentNode.clientHeight) { // ignore nodes higher than scroll area
        if (nodeRect.bottom > scrollParentNode.scrollTop + scrollParentNode.clientHeight) {
            node.scrollIntoView({behavior: 'smooth', block: 'end'});
        } else if (nodeRect.top < scrollParentNode.scrollTop) {
            node.scrollIntoView({behavior: 'smooth', block: 'start'});
        }
    }
    if (nodeRect.right - nodeRect.left <= scrollParentNode.clientWidth) { // ignore nodes wider than scroll area
        if (nodeRect.right > scrollParentNode.scrollLeft + scrollParentNode.clientWidth) {
            node.scrollIntoView({behavior: 'smooth', block: 'end'});
        } else if (nodeRect.left < scrollParentNode.scrollLeft) {
            node.scrollIntoView({behavior: 'smooth', block: 'end'});
        }
    }
};
