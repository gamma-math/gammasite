(function (global, factory) {
	typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
	typeof define === 'function' && define.amd ? define(['exports'], factory) :
	(global = global || self, factory(global.dawaAutocomplete = {}));
}(this, function (exports) { 'use strict';

	function unwrapExports (x) {
		return x && x.__esModule && Object.prototype.hasOwnProperty.call(x, 'default') ? x['default'] : x;
	}

	function createCommonjsModule(fn, module) {
		return module = { exports: {} }, fn(module, module.exports), module.exports;
	}

	var incrementalDomCjs = createCommonjsModule(function (module, exports) {

	Object.defineProperty(exports, '__esModule', { value: true });

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	var symbols = {
	  default: '__default'
	};

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	/**
	 * A cached reference to the hasOwnProperty function.
	 */
	var hasOwnProperty = Object.prototype.hasOwnProperty;
	/**
	 * A constructor function that will create blank objects.
	 */
	function Blank() {}
	Blank.prototype = Object.create(null);
	/**
	 * Used to prevent property collisions between our "map" and its prototype.
	 * @param map The map to check.
	 * @param property The property to check.
	 * @return Whether map has property.
	 */
	function has(map, property) {
	  return hasOwnProperty.call(map, property);
	}
	/**
	 * Creates an map object without a prototype.
	 */
	// tslint:disable-next-line:no-any
	function createMap() {
	  // tslint:disable-next-line:no-any
	  return new Blank();
	}
	/**
	 * Truncates an array, removing items up until length.
	 * @param arr The array to truncate.
	 * @param length The new length of the array.
	 */
	function truncateArray(arr, length) {
	  while (arr.length > length) {
	    arr.pop();
	  }
	}

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	/**
	 * Returns the namespace to use for the attribute.
	 */
	function getNamespace(name) {
	    if (name.lastIndexOf('xml:', 0) === 0) {
	        return 'http://www.w3.org/XML/1998/namespace';
	    }
	    if (name.lastIndexOf('xlink:', 0) === 0) {
	        return 'http://www.w3.org/1999/xlink';
	    }
	    return undefined;
	}
	/**
	 * Applies an attribute or property to a given Element. If the value is null
	 * or undefined, it is removed from the Element. Otherwise, the value is set
	 * as an attribute.
	 */
	// tslint:disable-next-line:no-any
	function applyAttr(el, name, value) {
	    if (value == null) {
	        el.removeAttribute(name);
	    } else {
	        var attrNS = getNamespace(name);
	        if (attrNS) {
	            el.setAttributeNS(attrNS, name, String(value));
	        } else {
	            el.setAttribute(name, String(value));
	        }
	    }
	}
	/**
	 * Applies a property to a given Element.
	 */
	// tslint:disable-next-line:no-any
	function applyProp(el, name, value) {
	    // tslint:disable-next-line:no-any
	    el[name] = value;
	}
	/**
	 * Applies a value to a style declaration. Supports CSS custom properties by
	 * setting properties containing a dash using CSSStyleDeclaration.setProperty.
	 */
	function setStyleValue(style, prop, value) {
	    if (prop.indexOf('-') >= 0) {
	        style.setProperty(prop, value);
	    } else {
	        // TODO(tomnguyen) Figure out why this is necessary.
	        // tslint:disable-next-line:no-any
	        style[prop] = value;
	    }
	}
	/**
	 * Applies a style to an Element. No vendor prefix expansion is done for
	 * property names/values.
	 * @param el
	 * @param name The attribute's name.
	 * @param  style The style to set. Either a string of css or an object
	 *     containing property-value pairs.
	 */
	function applyStyle(el, name, style) {
	    if (typeof style === 'string') {
	        el.style.cssText = style;
	    } else {
	        el.style.cssText = '';
	        var elStyle = el.style;
	        for (var prop in style) {
	            if (has(style, prop)) {
	                setStyleValue(elStyle, prop, style[prop]);
	            }
	        }
	    }
	}
	/**
	 * Updates a single attribute on an Element.
	 * @param el
	 * @param name The attribute's name.
	 * @param value The attribute's value. If the value is an object or
	 *     function it is set on the Element, otherwise, it is set as an HTML
	 *     attribute.
	 */
	function applyAttributeTyped(el, name, value) {
	    var type = typeof value;
	    if (type === 'object' || type === 'function') {
	        applyProp(el, name, value);
	    } else {
	        applyAttr(el, name, value);
	    }
	}
	/**
	 * A publicly mutable object to provide custom mutators for attributes.
	 * NB: The result of createMap() has to be recast since closure compiler
	 * will just assume attributes is "any" otherwise and throws away
	 * the type annotation set by tsickle.
	 */
	var attributes = createMap();
	// Special generic mutator that's called for any attribute that does not
	// have a specific mutator.
	attributes[symbols.default] = applyAttributeTyped;
	attributes['style'] = applyStyle;
	/**
	 * Calls the appropriate attribute mutator for this attribute.
	 */
	function updateAttribute(el, name, value) {
	    var mutator = attributes[name] || attributes[symbols.default];
	    mutator(el, name, value);
	}
	/**
	 * Asserts that a value exists and is not null or undefined. goog.asserts
	 * is not used in order to avoid dependencies on external code.
	 */
	function assert(val) {
	    return val;
	}

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	var notifications = {
	  nodesCreated: null,
	  nodesDeleted: null
	};

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	/**
	 * A context object keeps track of the state of a patch.
	 */
	var Context = /** @class */function () {
	    function Context() {
	        this.created = [];
	        this.deleted = [];
	    }
	    Context.prototype.markCreated = function (node) {
	        this.created.push(node);
	    };
	    Context.prototype.markDeleted = function (node) {
	        this.deleted.push(node);
	    };
	    /**
	     * Notifies about nodes that were created during the patch operation.
	     */
	    Context.prototype.notifyChanges = function () {
	        if (notifications.nodesCreated && this.created.length > 0) {
	            notifications.nodesCreated(this.created);
	        }
	        if (notifications.nodesDeleted && this.deleted.length > 0) {
	            notifications.nodesDeleted(this.deleted);
	        }
	    };
	    return Context;
	}();

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	/**
	 * Checks if the node is the root of a document. This is either a Document
	 * or ShadowRoot. DocumentFragments are included for simplicity of the
	 * implementation, though we only want to consider Documents or ShadowRoots.
	 * @param node The node to check.
	 * @return True if the node the root of a document, false otherwise.
	 */
	function isDocumentRoot(node) {
	    return node.nodeType === 11 || node.nodeType === 9;
	}
	/**
	 * Checks if the node is an Element. This is faster than an instanceof check.
	 * @param node The node to check.
	 * @return Whether or not the node is an Element.
	 */
	function isElement(node) {
	    return node.nodeType === 1;
	}
	/**
	 * Checks if the node is a text node. This is faster than an instanceof check.
	 * @param node The node to check.
	 * @return Whether or not the node is a Text.
	 */
	function isText(node) {
	    return node.nodeType === 3;
	}
	/**
	 * @param  node The node to start at, inclusive.
	 * @param  root The root ancestor to get until, exclusive.
	 * @return The ancestry of DOM nodes.
	 */
	function getAncestry(node, root) {
	    var ancestry = [];
	    var cur = node;
	    while (cur !== root) {
	        var n = cur;
	        ancestry.push(n);
	        cur = n.parentNode;
	    }
	    return ancestry;
	}
	/**
	 * return The root node of the DOM tree that contains this node.
	 */
	var getRootNode =
	// tslint:disable-next-line:no-any b/79476176
	Node.prototype.getRootNode || function () {
	    // tslint:disable-next-line:no-unnecessary-type-assertion b/77361044
	    var cur = this;
	    var prev = cur;
	    while (cur) {
	        prev = cur;
	        cur = cur.parentNode;
	    }
	    return prev;
	};
	/**
	 * @param node The node to get the activeElement for.
	 * @return The activeElement in the Document or ShadowRoot
	 *     corresponding to node, if present.
	 */
	function getActiveElement(node) {
	    var root = getRootNode.call(node);
	    return isDocumentRoot(root) ? root.activeElement : null;
	}
	/**
	 * Gets the path of nodes that contain the focused node in the same document as
	 * a reference node, up until the root.
	 * @param node The reference node to get the activeElement for.
	 * @param root The root to get the focused path until.
	 */
	function getFocusedPath(node, root) {
	    var activeElement = getActiveElement(node);
	    if (!activeElement || !node.contains(activeElement)) {
	        return [];
	    }
	    return getAncestry(activeElement, root);
	}
	/**
	 * Like insertBefore, but instead instead of moving the desired node, instead
	 * moves all the other nodes after.
	 * @param parentNode
	 * @param node
	 * @param referenceNode
	 */
	function moveBefore(parentNode, node, referenceNode) {
	    var insertReferenceNode = node.nextSibling;
	    var cur = referenceNode;
	    while (cur !== null && cur !== node) {
	        var next = cur.nextSibling;
	        parentNode.insertBefore(cur, insertReferenceNode);
	        cur = next;
	    }
	}

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	/**
	 * Keeps track of information needed to perform diffs for a given DOM node.
	 */
	var NodeData = /** @class */function () {
	    function NodeData(nameOrCtor, key, text) {
	        /**
	         * An array of attribute name/value pairs, used for quickly diffing the
	         * incomming attributes to see if the DOM node's attributes need to be
	         * updated.
	         */
	        // tslint:disable-next-line:no-any
	        this._attrsArr = null;
	        /**
	         * Whether or not the statics have been applied for the node yet.
	         */
	        this.staticsApplied = false;
	        this.nameOrCtor = nameOrCtor;
	        this.key = key;
	        this.text = text;
	    }
	    NodeData.prototype.hasEmptyAttrsArr = function () {
	        var attrs = this._attrsArr;
	        return !attrs || !attrs.length;
	    };
	    NodeData.prototype.getAttrsArr = function (length) {
	        return this._attrsArr || (this._attrsArr = new Array(length));
	    };
	    return NodeData;
	}();
	/**
	 * Initializes a NodeData object for a Node.
	 */
	function initData(node, nameOrCtor, key, text) {
	    var data = new NodeData(nameOrCtor, key, text);
	    node['__incrementalDOMData'] = data;
	    return data;
	}
	/**
	 * Retrieves the NodeData object for a Node, creating it if necessary.
	 */
	function getData(node, key) {
	    return importSingleNode(node, key);
	}
	function isDataInitialized(node) {
	    return Boolean(node['__incrementalDOMData']);
	}
	function getKey(node) {
	    assert(node['__incrementalDOMData']);
	    return getData(node).key;
	}
	/**
	 * Imports single node and its subtree, initializing caches.
	 */
	function importSingleNode(node, fallbackKey) {
	    if (node['__incrementalDOMData']) {
	        return node['__incrementalDOMData'];
	    }
	    var nodeName = isElement(node) ? node.localName : node.nodeName;
	    var key = isElement(node) ? node.getAttribute('key') || fallbackKey : null;
	    var text = isText(node) ? node.data : undefined;
	    var data = initData(node, nodeName, key, text);
	    if (isElement(node)) {
	        recordAttributes(node, data);
	    }
	    return data;
	}
	/**
	 * Imports node and its subtree, initializing caches.
	 */
	function importNode(node) {
	    importSingleNode(node);
	    for (var child = node.firstChild; child; child = child.nextSibling) {
	        importNode(child);
	    }
	}
	/**
	 * Clears all caches from a node and all of its children.
	 */
	function clearCache(node) {
	    node['__incrementalDOMData'] = null;
	    for (var child = node.firstChild; child; child = child.nextSibling) {
	        clearCache(child);
	    }
	}
	/**
	 * Records the element's attributes.
	 * @param node The Element that may have attributes
	 * @param data The Element's data
	 */
	function recordAttributes(node, data) {
	    var attributes = node.attributes;
	    var length = attributes.length;
	    if (!length) {
	        return;
	    }
	    var attrsArr = data.getAttrsArr(length);
	    // Use a cached length. The attributes array is really a live NamedNodeMap,
	    // which exists as a DOM "Host Object" (probably as C++ code). This makes the
	    // usual constant length iteration very difficult to optimize in JITs.
	    for (var i = 0, j = 0; i < length; i += 1, j += 2) {
	        var attr = attributes[i];
	        var name = attr.name;
	        var value = attr.value;
	        attrsArr[j] = name;
	        attrsArr[j + 1] = value;
	    }
	}

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	/**
	 * Gets the namespace to create an element (of a given tag) in.
	 */
	function getNamespaceForTag(tag, parent) {
	    if (tag === 'svg') {
	        return 'http://www.w3.org/2000/svg';
	    }
	    if (tag === 'math') {
	        return 'http://www.w3.org/1998/Math/MathML';
	    }
	    if (parent == null) {
	        return null;
	    }
	    if (getData(parent).nameOrCtor === 'foreignObject') {
	        return null;
	    }
	    return parent.namespaceURI;
	}
	/**
	 * Creates an Element.
	 * @param doc The document with which to create the Element.
	 * @param nameOrCtor The tag or constructor for the Element.
	 * @param key A key to identify the Element.
	 * @param  typeId The type identifier for the Element.
	 */
	function createElement(doc, parent, nameOrCtor, key) {
	    var el;
	    if (typeof nameOrCtor === 'function') {
	        el = new nameOrCtor();
	    } else {
	        var namespace = getNamespaceForTag(nameOrCtor, parent);
	        if (namespace) {
	            el = doc.createElementNS(namespace, nameOrCtor);
	        } else {
	            el = doc.createElement(nameOrCtor);
	        }
	    }
	    initData(el, nameOrCtor, key);
	    return el;
	}
	/**
	 * Creates a Text Node.
	 * @param doc The document with which to create the Element.
	 * @return
	 */
	function createText(doc) {
	    var node = doc.createTextNode('');
	    initData(node, '#text', null);
	    return node;
	}

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	var context = null;
	var currentNode = null;
	var currentParent = null;
	var doc = null;
	var focusPath = [];
	/**
	 * Used to build up call arguments. Each patch call gets a separate copy, so
	 * this works with nested calls to patch.
	 */
	var argsBuilder = [];
	/**
	 * TODO(sparhami) We should just export argsBuilder directly when Closure
	 * Compiler supports ES6 directly.
	 */
	function getArgsBuilder() {
	    return argsBuilder;
	}
	/**
	 * Returns a patcher function that sets up and restores a patch context,
	 * running the run function with the provided data.
	 */
	function patchFactory(run) {
	    var f = function (node, fn, data) {
	        var prevContext = context;
	        var prevDoc = doc;
	        var prevFocusPath = focusPath;
	        var prevArgsBuilder = argsBuilder;
	        var prevCurrentNode = currentNode;
	        var prevCurrentParent = currentParent;
	        context = new Context();
	        doc = node.ownerDocument;
	        argsBuilder = [];
	        currentParent = node.parentNode;
	        focusPath = getFocusedPath(node, currentParent);
	        try {
	            var retVal = run(node, fn, data);
	            return retVal;
	        } finally {
	            doc = prevDoc;
	            argsBuilder = prevArgsBuilder;
	            currentNode = prevCurrentNode;
	            currentParent = prevCurrentParent;
	            focusPath = prevFocusPath;
	            context.notifyChanges();
	            context = prevContext;
	        }
	    };
	    return f;
	}
	/**
	 * Patches the document starting at node with the provided function. This
	 * function may be called during an existing patch operation.
	 */
	var patchInner = patchFactory(function (node, fn, data) {
	    currentNode = node;
	    enterNode();
	    fn(data);
	    exitNode();
	    return node;
	});
	/**
	 * Patches an Element with the the provided function. Exactly one top level
	 * element call should be made corresponding to `node`.
	 */
	var patchOuter = patchFactory(function (node, fn, data) {
	    // tslint:disable-next-line:no-any
	    var startNode = { nextSibling: node };
	    currentNode = startNode;
	    fn(data);
	    if (currentParent) {
	        clearUnvisitedDOM(currentParent, getNextNode(), node.nextSibling);
	    }
	    return startNode === currentNode ? null : currentNode;
	});
	/**
	 * Checks whether or not the current node matches the specified nameOrCtor and
	 * key.
	 * @param matchNode A node to match the data to.
	 * @param nameOrCtor The name or constructor to check for.
	 * @param key The key used to identify the Node.
	 * @return True if the node matches, false otherwise.
	 */
	function matches(matchNode, nameOrCtor, key) {
	    var data = getData(matchNode, key);
	    // Key check is done using double equals as we want to treat a null key the
	    // same as undefined. This should be okay as the only values allowed are
	    // strings, null and undefined so the == semantics are not too weird.
	    // tslint:disable-next-line:triple-equals
	    return nameOrCtor == data.nameOrCtor && key == data.key;
	}
	/**
	 * Finds the matching node, starting at `node` and looking at the subsequent
	 * siblings if a key is used.
	 * @param node The node to start looking at.
	 * @param nameOrCtor The name or constructor for the Node.
	 * @param key The key used to identify the Node.
	 */
	function getMatchingNode(matchNode, nameOrCtor, key) {
	    if (!matchNode) {
	        return null;
	    }
	    if (matches(matchNode, nameOrCtor, key)) {
	        return matchNode;
	    }
	    if (key) {
	        while (matchNode = matchNode.nextSibling) {
	            if (matches(matchNode, nameOrCtor, key)) {
	                return matchNode;
	            }
	        }
	    }
	    return null;
	}
	/**
	 * Creates a Node and marking it as created.
	 * @param nameOrCtor The name or constructor for the Node.
	 * @param key The key used to identify the Node.
	 * @return The newly created node.
	 */
	function createNode(nameOrCtor, key) {
	    var node;
	    if (nameOrCtor === '#text') {
	        node = createText(doc);
	    } else {
	        node = createElement(doc, currentParent, nameOrCtor, key);
	    }
	    context.markCreated(node);
	    return node;
	}
	/**
	 * Aligns the virtual Node definition with the actual DOM, moving the
	 * corresponding DOM node to the correct location or creating it if necessary.
	 * @param nameOrCtor The name or constructor for the Node.
	 * @param key The key used to identify the Node.
	 */
	function alignWithDOM(nameOrCtor, key) {
	    var existingNode = getMatchingNode(currentNode, nameOrCtor, key);
	    var node = existingNode || createNode(nameOrCtor, key);
	    // If we are at the matching node, then we are done.
	    if (node === currentNode) {
	        return;
	    }
	    // Re-order the node into the right position, preserving focus if either
	    // node or currentNode are focused by making sure that they are not detached
	    // from the DOM.
	    if (focusPath.indexOf(node) >= 0) {
	        // Move everything else before the node.
	        moveBefore(currentParent, node, currentNode);
	    } else {
	        currentParent.insertBefore(node, currentNode);
	    }
	    currentNode = node;
	}
	/**
	 * Clears out any unvisited Nodes in a given range.
	 * @param maybeParentNode
	 * @param startNode The node to start clearing from, inclusive.
	 * @param endNode The node to clear until, exclusive.
	 */
	function clearUnvisitedDOM(maybeParentNode, startNode, endNode) {
	    var parentNode = maybeParentNode;
	    var child = startNode;
	    while (child !== endNode) {
	        var next = child.nextSibling;
	        parentNode.removeChild(child);
	        context.markDeleted(child);
	        child = next;
	    }
	}
	/**
	 * Changes to the first child of the current node.
	 */
	function enterNode() {
	    currentParent = currentNode;
	    currentNode = null;
	}
	/**
	 * @return The next Node to be patched.
	 */
	function getNextNode() {
	    if (currentNode) {
	        return currentNode.nextSibling;
	    } else {
	        return currentParent.firstChild;
	    }
	}
	/**
	 * Changes to the next sibling of the current node.
	 */
	function nextNode() {
	    currentNode = getNextNode();
	}
	/**
	 * Changes to the parent of the current node, removing any unvisited children.
	 */
	function exitNode() {
	    clearUnvisitedDOM(currentParent, getNextNode(), null);
	    currentNode = currentParent;
	    currentParent = currentParent.parentNode;
	}
	/**
	 * Makes sure that the current node is an Element with a matching nameOrCtor and
	 * key.
	 *
	 * @param nameOrCtor The tag or constructor for the Element.
	 * @param key The key used to identify this element. This can be an
	 *     empty string, but performance may be better if a unique value is used
	 *     when iterating over an array of items.
	 * @return The corresponding Element.
	 */
	function open(nameOrCtor, key) {
	    nextNode();
	    alignWithDOM(nameOrCtor, key);
	    enterNode();
	    return currentParent;
	}
	/**
	 * Closes the currently open Element, removing any unvisited children if
	 * necessary.
	 */
	function close() {
	    exitNode();
	    return currentNode;
	}
	/**
	 * Makes sure the current node is a Text node and creates a Text node if it is
	 * not.
	 */
	function text() {
	    nextNode();
	    alignWithDOM('#text', null);
	    return currentNode;
	}
	/**
	 * Gets the current Element being patched.
	 */
	function currentElement() {
	    return currentParent;
	}
	/**
	 * @return The Node that will be evaluated for the next instruction.
	 */
	function currentPointer() {
	    // TODO(tomnguyen): assert that this is not null
	    return getNextNode();
	}
	/**
	 * Skips the children in a subtree, allowing an Element to be closed without
	 * clearing out the children.
	 */
	function skip() {
	    currentNode = currentParent.lastChild;
	}

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */
	/**
	 * The offset in the virtual element declaration where the attributes are
	 * specified.
	 */
	var ATTRIBUTES_OFFSET = 3;
	/**
	 * Used to keep track of the previous values when a 2-way diff is necessary.
	 * This object is reused.
	 * TODO(sparhamI) Scope this to a patch so you can call patch from an attribute
	 * update.
	 */
	var prevAttrsMap = createMap();
	/**
	 * Applies the statics. When importing an Element, any existing attributes that
	 * match a static are converted into a static attribute.
	 * @param node The Element to apply statics for.
	 * @param data The Element's data
	 * @param statics The statics array,
	 */
	function applyStatics(node, data, statics) {
	    data.staticsApplied = true;
	    if (!statics || !statics.length) {
	        return;
	    }
	    if (data.hasEmptyAttrsArr()) {
	        for (var i = 0; i < statics.length; i += 2) {
	            updateAttribute(node, statics[i], statics[i + 1]);
	        }
	        return;
	    }
	    for (var i = 0; i < statics.length; i += 2) {
	        prevAttrsMap[statics[i]] = i + 1;
	    }
	    var attrsArr = data.getAttrsArr(0);
	    var j = 0;
	    for (var i = 0; i < attrsArr.length; i += 2) {
	        var name = attrsArr[i];
	        var value = attrsArr[i + 1];
	        var staticsIndex = prevAttrsMap[name];
	        if (staticsIndex) {
	            // For any attrs that are static and have the same value, make sure we do
	            // not set them again.
	            if (statics[staticsIndex] === value) {
	                delete prevAttrsMap[name];
	            }
	            continue;
	        }
	        // For any attrs that are dynamic, move them up to the right place.
	        attrsArr[j] = name;
	        attrsArr[j + 1] = value;
	        j += 2;
	    }
	    // Anything after `j` was either moved up already or static.
	    truncateArray(attrsArr, j);
	    for (var name in prevAttrsMap) {
	        updateAttribute(node, name, statics[prevAttrsMap[name]]);
	        delete prevAttrsMap[name];
	    }
	}
	/**
	 * @param  nameOrCtor The Element's tag or constructor.
	 * @param  key The key used to identify this element. This can be an
	 *     empty string, but performance may be better if a unique value is used
	 *     when iterating over an array of items.
	 * @param statics An array of attribute name/value pairs of the static
	 *     attributes for the Element. Attributes will only be set once when the
	 *     Element is created.
	 * @param varArgs, Attribute name/value pairs of the dynamic attributes
	 *     for the Element.
	 * @return The corresponding Element.
	 */
	function elementOpen(nameOrCtor, key,
	// Ideally we could tag statics and varArgs as an array where every odd
	// element is a string and every even element is any, but this is hard.
	// tslint:disable-next-line:no-any
	statics) {
	    var varArgs = [];
	    for (var _i = 3; _i < arguments.length; _i++) {
	        varArgs[_i - 3] = arguments[_i];
	    }
	    var node = open(nameOrCtor, key);
	    var data = getData(node);
	    if (!data.staticsApplied) {
	        applyStatics(node, data, statics);
	    }
	    var attrsLength = Math.max(0, arguments.length - ATTRIBUTES_OFFSET);
	    var hadNoAttrs = data.hasEmptyAttrsArr();
	    if (!attrsLength && hadNoAttrs) {
	        return node;
	    }
	    var attrsArr = data.getAttrsArr(attrsLength);
	    /*
	     * Checks to see if one or more attributes have changed for a given Element.
	     * When no attributes have changed, this is much faster than checking each
	     * individual argument. When attributes have changed, the overhead of this is
	     * minimal.
	     */
	    var i = ATTRIBUTES_OFFSET;
	    var j = 0;
	    for (; i < arguments.length; i += 2, j += 2) {
	        var name = arguments[i];
	        if (hadNoAttrs) {
	            attrsArr[j] = name;
	        } else if (attrsArr[j] !== name) {
	            break;
	        }
	        var value = arguments[i + 1];
	        if (hadNoAttrs || attrsArr[j + 1] !== value) {
	            attrsArr[j + 1] = value;
	            updateAttribute(node, name, value);
	        }
	    }
	    /*
	     * Items did not line up exactly as before, need to make sure old items are
	     * removed. This can happen if using conditional logic when declaring
	     * attrs through the elementOpenStart flow or if one element is reused in
	     * the place of another.
	     */
	    if (i < arguments.length || j < attrsArr.length) {
	        var attrsStart = j;
	        for (; j < attrsArr.length; j += 2) {
	            prevAttrsMap[attrsArr[j]] = attrsArr[j + 1];
	        }
	        for (j = attrsStart; i < arguments.length; i += 2, j += 2) {
	            var name = arguments[i];
	            var value = arguments[i + 1];
	            if (prevAttrsMap[name] !== value) {
	                updateAttribute(node, name, value);
	            }
	            attrsArr[j] = name;
	            attrsArr[j + 1] = value;
	            delete prevAttrsMap[name];
	        }
	        truncateArray(attrsArr, j);
	        /*
	         * At this point, only have attributes that were present before, but have
	         * been removed.
	         */
	        for (var name in prevAttrsMap) {
	            updateAttribute(node, name, undefined);
	            delete prevAttrsMap[name];
	        }
	    }
	    return node;
	}
	/**
	 * Declares a virtual Element at the current location in the document. This
	 * corresponds to an opening tag and a elementClose tag is required. This is
	 * like elementOpen, but the attributes are defined using the attr function
	 * rather than being passed as arguments. Must be folllowed by 0 or more calls
	 * to attr, then a call to elementOpenEnd.
	 * @param nameOrCtor The Element's tag or constructor.
	 * @param key The key used to identify this element. This can be an
	 *     empty string, but performance may be better if a unique value is used
	 *     when iterating over an array of items.
	 * @param statics An array of attribute name/value pairs of the static
	 *     attributes for the Element. Attributes will only be set once when the
	 *     Element is created.
	 */
	function elementOpenStart(nameOrCtor, key, statics) {
	    var argsBuilder = getArgsBuilder();
	    argsBuilder[0] = nameOrCtor;
	    argsBuilder[1] = key;
	    argsBuilder[2] = statics;
	}
	/**
	 * Allows you to define a key after an elementOpenStart. This is useful in
	 * templates that define key after an element has been opened ie
	 * `<div key('foo')></div>`.
	 */
	function key(key) {
	    var argsBuilder = getArgsBuilder();
	    argsBuilder[1] = key;
	}
	/***
	 * Defines a virtual attribute at this point of the DOM. This is only valid
	 * when called between elementOpenStart and elementOpenEnd.
	 */
	// tslint:disable-next-line:no-any
	function attr(name, value) {
	    var argsBuilder = getArgsBuilder();
	    argsBuilder.push(name);
	    argsBuilder.push(value);
	}
	/**
	 * Closes an open tag started with elementOpenStart.
	 * @return The corresponding Element.
	 */
	function elementOpenEnd() {
	    var argsBuilder = getArgsBuilder();
	    var node = elementOpen.apply(null, argsBuilder);
	    truncateArray(argsBuilder, 0);
	    return node;
	}
	/**
	 * Closes an open virtual Element.
	 *
	 * @param nameOrCtor The Element's tag or constructor.
	 * @return The corresponding Element.
	 */
	function elementClose(nameOrCtor) {
	    var node = close();
	    return node;
	}
	/**
	 * Declares a virtual Element at the current location in the document that has
	 * no children.
	 * @param nameOrCtor The Element's tag or constructor.
	 * @param key The key used to identify this element. This can be an
	 *     empty string, but performance may be better if a unique value is used
	 *     when iterating over an array of items.
	 * @param statics An array of attribute name/value pairs of the static
	 *     attributes for the Element. Attributes will only be set once when the
	 *     Element is created.
	 * @param varArgs Attribute name/value pairs of the dynamic attributes
	 *     for the Element.
	 * @return The corresponding Element.
	 */
	function elementVoid(nameOrCtor, key,
	// Ideally we could tag statics and varArgs as an array where every odd
	// element is a string and every even element is any, but this is hard.
	// tslint:disable-next-line:no-any
	statics) {
	    var varArgs = [];
	    for (var _i = 3; _i < arguments.length; _i++) {
	        varArgs[_i - 3] = arguments[_i];
	    }
	    elementOpen.apply(null, arguments);
	    return elementClose(nameOrCtor);
	}
	/**
	 * Declares a virtual Text at this point in the document.
	 *
	 * @param value The value of the Text.
	 * @param varArgs
	 *     Functions to format the value which are called only when the value has
	 *     changed.
	 * @return The corresponding text node.
	 */
	function text$1(value) {
	    var varArgs = [];
	    for (var _i = 1; _i < arguments.length; _i++) {
	        varArgs[_i - 1] = arguments[_i];
	    }
	    var node = text();
	    var data = getData(node);
	    if (data.text !== value) {
	        data.text = value;
	        var formatted = value;
	        for (var i = 1; i < arguments.length; i += 1) {
	            /*
	             * Call the formatter function directly to prevent leaking arguments.
	             * https://github.com/google/incremental-dom/pull/204#issuecomment-178223574
	             */
	            var fn = arguments[i];
	            formatted = fn(formatted);
	        }
	        node.data = formatted;
	    }
	    return node;
	}

	/**
	 * @license
	 * Copyright 2018 The Incremental DOM Authors. All Rights Reserved.
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 *      http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS-IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	exports.applyAttr = applyAttr;
	exports.applyProp = applyProp;
	exports.attributes = attributes;
	exports.close = close;
	exports.currentElement = currentElement;
	exports.currentPointer = currentPointer;
	exports.open = open;
	exports.patch = patchInner;
	exports.patchInner = patchInner;
	exports.patchOuter = patchOuter;
	exports.skip = skip;
	exports.skipNode = nextNode;
	exports.getKey = getKey;
	exports.clearCache = clearCache;
	exports.importNode = importNode;
	exports.isDataInitialized = isDataInitialized;
	exports.notifications = notifications;
	exports.symbols = symbols;
	exports.attr = attr;
	exports.elementClose = elementClose;
	exports.elementOpen = elementOpen;
	exports.elementOpenEnd = elementOpenEnd;
	exports.elementOpenStart = elementOpenStart;
	exports.elementVoid = elementVoid;
	exports.text = text$1;
	exports.key = key;


	});

	unwrapExports(incrementalDomCjs);
	var incrementalDomCjs_1 = incrementalDomCjs.applyAttr;
	var incrementalDomCjs_2 = incrementalDomCjs.applyProp;
	var incrementalDomCjs_3 = incrementalDomCjs.attributes;
	var incrementalDomCjs_4 = incrementalDomCjs.close;
	var incrementalDomCjs_5 = incrementalDomCjs.currentElement;
	var incrementalDomCjs_6 = incrementalDomCjs.currentPointer;
	var incrementalDomCjs_7 = incrementalDomCjs.open;
	var incrementalDomCjs_8 = incrementalDomCjs.patch;
	var incrementalDomCjs_9 = incrementalDomCjs.patchInner;
	var incrementalDomCjs_10 = incrementalDomCjs.patchOuter;
	var incrementalDomCjs_11 = incrementalDomCjs.skip;
	var incrementalDomCjs_12 = incrementalDomCjs.skipNode;
	var incrementalDomCjs_13 = incrementalDomCjs.getKey;
	var incrementalDomCjs_14 = incrementalDomCjs.clearCache;
	var incrementalDomCjs_15 = incrementalDomCjs.importNode;
	var incrementalDomCjs_16 = incrementalDomCjs.isDataInitialized;
	var incrementalDomCjs_17 = incrementalDomCjs.notifications;
	var incrementalDomCjs_18 = incrementalDomCjs.symbols;
	var incrementalDomCjs_19 = incrementalDomCjs.attr;
	var incrementalDomCjs_20 = incrementalDomCjs.elementClose;
	var incrementalDomCjs_21 = incrementalDomCjs.elementOpen;
	var incrementalDomCjs_22 = incrementalDomCjs.elementOpenEnd;
	var incrementalDomCjs_23 = incrementalDomCjs.elementOpenStart;
	var incrementalDomCjs_24 = incrementalDomCjs.elementVoid;
	var incrementalDomCjs_25 = incrementalDomCjs.text;
	var incrementalDomCjs_26 = incrementalDomCjs.key;

	incrementalDomCjs_3.caretpos = function (element, name, value) {
	  element.setSelectionRange(value, value);
	};

	incrementalDomCjs_3.value = incrementalDomCjs_2;

	var renderIncrementalDOM = function renderIncrementalDOM(data, onSelect, multiline) {
	  if (data.suggestions.length > 0 && data.focused) {
	    // unselectable=on is a IE11 workaround,
	    // which makes it possible to use an eventual scroll bar on suggestions list.
	    incrementalDomCjs_21('ul', '', ['class', 'dawa-autocomplete-suggestions', 'role', 'listbox', 'unselectable', 'on']);

	    var _loop = function _loop(i) {
	      var suggestion = data.suggestions[i];
	      var className = 'dawa-autocomplete-suggestion';

	      if (data.selected === i) {
	        className += ' dawa-selected';
	      }

	      incrementalDomCjs_21('li', '', ['role', 'option'], 'class', className, 'onmousedown', function (e) {
	        onSelect(i);
	        e.preventDefault();
	      });
	      var rows = suggestion.forslagstekst.split('\n');
	      rows = rows.map(function (row) {
	        return row.replace(/ /g, "\xA0");
	      });

	      if (multiline) {
	        incrementalDomCjs_25(rows[0]);

	        for (var _i = 1; _i < rows.length; ++_i) {
	          incrementalDomCjs_24('br');
	          incrementalDomCjs_25(rows[_i]);
	        }
	      } else {
	        incrementalDomCjs_25(rows.join(', '));
	      }

	      incrementalDomCjs_20('li');
	    };

	    for (var i = 0; i < data.suggestions.length; ++i) {
	      _loop(i);
	    }

	    incrementalDomCjs_20('ul');
	  }
	};

	var defaultRender = function defaultRender(containerElm, data, onSelect, multiline) {
	  incrementalDomCjs_8(containerElm, function () {
	    renderIncrementalDOM(data, onSelect, multiline);
	  });
	};

	var autocompleteUi = function autocompleteUi(inputElm, options) {
	  var onSelect = options.onSelect;
	  var onTextChange = options.onTextChange;
	  var render = options.render || defaultRender;
	  var destroyed = false;
	  var lastEmittedText = '';
	  var lastEmittedCaretpos = 0;
	  var suggestionContainerElm = document.createElement('div');
	  inputElm.parentNode.insertBefore(suggestionContainerElm, inputElm.nextSibling);

	  var emitTextChange = function emitTextChange(newText, newCaretpos) {
	    if (lastEmittedText !== newText || lastEmittedCaretpos !== newCaretpos) {
	      onTextChange(newText, newCaretpos);
	      lastEmittedText = newText;
	      lastEmittedCaretpos = newCaretpos;
	    }
	  };

	  var data = {
	    caretpos: 2,
	    inputText: '',
	    selected: 0,
	    focused: document.activeElement === inputElm,
	    suggestions: []
	  };

	  var handleInputChanged = function handleInputChanged(inputElm) {
	    var newText = inputElm.value;
	    var newCaretPos = inputElm.selectionStart;
	    data.caretpos = newCaretPos;
	    data.inputText = newText;
	    emitTextChange(newText, newCaretPos);
	  };

	  var update;

	  var selectSuggestion = function selectSuggestion(index) {
	    var selectedSuggestion = data.suggestions[index];
	    data.inputText = selectedSuggestion.tekst;
	    data.caretpos = selectedSuggestion.caretpos;
	    data.suggestions = [];
	    onSelect(selectedSuggestion);
	    update(true);
	  };

	  var keydownHandler = function keydownHandler(e) {
	    var key = window.event ? e.keyCode : e.which;

	    if (data.suggestions.length > 0 && data.focused) {
	      // down (40)
	      if (key === 40) {
	        data.selected = (data.selected + 1) % data.suggestions.length;
	        update();
	      } //up (38)
	      else if (key === 38) {
	          data.selected = (data.selected - 1 + data.suggestions.length) % data.suggestions.length;
	          update();
	        } // enter
	        else if (key === 13 || key === 9) {
	            selectSuggestion(data.selected);
	          } else {
	            return true;
	          }

	      e.preventDefault();
	      return false;
	    }
	  };

	  var focusHandler = function focusHandler() {
	    data.focused = true;
	    update();
	  };

	  var blurHandler = function blurHandler() {
	    data.focused = false;
	    update();
	    return false;
	  };

	  var inputChangeHandler = function inputChangeHandler(e) {
	    handleInputChanged(e.target);
	  };

	  var inputMouseUpHandler = function inputMouseUpHandler(e) {
	    return handleInputChanged(e.target);
	  };

	  var updateScheduled = false;
	  var updateInput = false;

	  update = function update(shouldUpdateInput) {
	    if (shouldUpdateInput) {
	      updateInput = true;
	    }

	    if (!updateScheduled) {
	      updateScheduled = true;
	      requestAnimationFrame(function () {
	        if (destroyed) {
	          return;
	        }

	        updateScheduled = false;

	        if (updateInput) {
	          inputElm.value = data.inputText;
	          inputElm.setSelectionRange(data.caretpos, data.caretpos);
	        }

	        updateInput = false;
	        render(suggestionContainerElm, data, function (i) {
	          return selectSuggestion(i);
	        }, options.multiline);
	      });
	    }
	  };

	  update();

	  var destroy = function destroy() {
	    destroyed = true;
	    inputElm.removeEventListener('keydown', keydownHandler);
	    inputElm.removeEventListener('blur', blurHandler);
	    inputElm.removeEventListener('focus', focusHandler);
	    inputElm.removeEventListener('input', inputChangeHandler);
	    inputElm.removeEventListener('mouseup', inputMouseUpHandler);
	    incrementalDomCjs_8(suggestionContainerElm, function () {});
	    suggestionContainerElm.remove();
	  };

	  var setSuggestions = function setSuggestions(suggestions) {
	    data.suggestions = suggestions;
	    data.selected = 0;
	    update();
	  };

	  var selectAndClose = function selectAndClose(text) {
	    data.inputText = text;
	    data.caretpos = text.length;
	    data.suggestions = [];
	    data.selected = 0;
	    update(true);
	  };

	  inputElm.addEventListener('keydown', keydownHandler);
	  inputElm.addEventListener('blur', blurHandler);
	  inputElm.addEventListener('focus', focusHandler);
	  inputElm.addEventListener('input', inputChangeHandler);
	  inputElm.addEventListener('mouseup', inputMouseUpHandler);
	  inputElm.setAttribute('aria-autocomplete', 'list');
	  inputElm.setAttribute('autocomplete', 'off');
	  return {
	    destroy: destroy,
	    setSuggestions: setSuggestions,
	    selectAndClose: selectAndClose
	  };
	};

	function _classCallCheck(instance, Constructor) {
	  if (!(instance instanceof Constructor)) {
	    throw new TypeError("Cannot call a class as a function");
	  }
	}

	function _defineProperties(target, props) {
	  for (var i = 0; i < props.length; i++) {
	    var descriptor = props[i];
	    descriptor.enumerable = descriptor.enumerable || false;
	    descriptor.configurable = true;
	    if ("value" in descriptor) descriptor.writable = true;
	    Object.defineProperty(target, descriptor.key, descriptor);
	  }
	}

	function _createClass(Constructor, protoProps, staticProps) {
	  if (protoProps) _defineProperties(Constructor.prototype, protoProps);
	  if (staticProps) _defineProperties(Constructor, staticProps);
	  return Constructor;
	}

	function fetch(e,n){return n=n||{},new Promise(function(t,r){var s=new XMLHttpRequest,o=[],u=[],i={},a=function(){return {ok:2==(s.status/100|0),statusText:s.statusText,status:s.status,url:s.responseURL,text:function(){return Promise.resolve(s.responseText)},json:function(){return Promise.resolve(JSON.parse(s.responseText))},blob:function(){return Promise.resolve(new Blob([s.response]))},clone:a,headers:{keys:function(){return o},entries:function(){return u},get:function(e){return i[e.toLowerCase()]},has:function(e){return e.toLowerCase()in i}}}};for(var l in s.open(n.method||"get",e,!0),s.onload=function(){s.getAllResponseHeaders().replace(/^(.*?):[^\S\n]*([\s\S]*?)$/gm,function(e,n,t){o.push(n=n.toLowerCase()),u.push([n,t]),i[n]=i[n]?i[n]+","+t:t;}),t(a());},s.onerror=r,s.withCredentials="include"==n.credentials,n.headers)s.setRequestHeader(l,n.headers[l]);s.send(n.body||null);})}

	var formatParams = function formatParams(params) {
	  return Object.keys(params).map(function (paramName) {
	    var paramValue = params[paramName];
	    return "".concat(paramName, "=").concat(encodeURIComponent(paramValue));
	  }).join('&');
	};

	var delay = function delay(ms) {
	  return new Promise(function (resolve) {
	    return setTimeout(resolve, ms);
	  });
	};

	var defaultOptions = {
	  params: {},
	  minLength: 2,
	  retryDelay: 500,
	  renderCallback: function renderCallback() {
	    /*eslint no-console: 0*/
	    console.error('No renderCallback supplied');
	  },
	  initialRenderCallback: function initialRenderCallback() {
	    /*eslint no-console: 0*/
	    console.error('No initialRenderCallback supplied');
	  },
	  type: 'adresse',
	  baseUrl: 'https://dawa.aws.dk',
	  adgangsadresserOnly: false,
	  stormodtagerpostnumre: true,
	  supplerendebynavn: true,
	  fuzzy: true,
	  fetchImpl: function fetchImpl(url, params) {
	    return fetch("".concat(url, "?").concat(formatParams(params)), {
	      mode: 'cors'
	    }).then(function (result) {
	      return result.json();
	    });
	  }
	};
	var AutocompleteController =
	/*#__PURE__*/
	function () {
	  function AutocompleteController(options) {
	    _classCallCheck(this, AutocompleteController);

	    options = Object.assign({}, defaultOptions, options || {});
	    this.options = options;
	    this.state = {
	      currentRequest: null,
	      pendingRequest: null
	    };
	    this.selected = null;
	  }

	  _createClass(AutocompleteController, [{
	    key: "_getAutocompleteResponse",
	    value: function _getAutocompleteResponse(text, caretpos, skipVejnavn, adgangsadresseid, supplerendebynavn, stormodtagerpostnumre) {
	      var params = Object.assign({}, this.options.params, {
	        q: text,
	        type: this.options.type,
	        caretpos: caretpos,
	        supplerendebynavn: supplerendebynavn,
	        stormodtagerpostnumre: stormodtagerpostnumre,
	        multilinje: true
	      });

	      if (this.options.fuzzy) {
	        params.fuzzy = '';
	      }

	      if (adgangsadresseid) {
	        params.adgangsadresseid = adgangsadresseid;
	      }

	      if (skipVejnavn) {
	        params.startfra = 'adgangsadresse';
	      }

	      return this.options.fetchImpl("".concat(this.options.baseUrl, "/autocomplete"), params);
	    }
	  }, {
	    key: "_scheduleRequest",
	    value: function _scheduleRequest(request) {
	      if (this.state.currentRequest !== null) {
	        this.state.pendingRequest = request;
	      } else {
	        this.state.currentRequest = request;

	        this._executeRequest();
	      }
	    }
	  }, {
	    key: "_executeRequest",
	    value: function _executeRequest() {
	      var _this = this;

	      var request = this.state.currentRequest;
	      var adgangsadresseid = null;
	      var skipVejnavn = false;
	      var text, caretpos;

	      if (request.selected) {
	        var item = request.selected;

	        if (item.type !== this.options.type) {
	          adgangsadresseid = item.type === 'adgangsadresse' ? item.data.id : null;
	          skipVejnavn = item.type === 'vejnavn';
	          text = item.tekst;
	          caretpos = item.caretpos;
	        } else {
	          this.options.selectCallback(item);
	          this.selected = item;

	          this._requestCompleted();

	          return;
	        }
	      } else {
	        text = request.text;
	        caretpos = request.caretpos;
	      }

	      if (request.selectedId) {
	        var params = {
	          id: request.selectedId,
	          type: this.options.type
	        };
	        return this.options.fetchImpl("".concat(this.options.baseUrl, "/autocomplete"), params).then(function (result) {
	          return _this._handleResponse(request, result);
	        }, function (error) {
	          return _this._handleFailedRequest(request, error);
	        });
	      } else if (request.selected || request.text.length >= this.options.minLength) {
	        this._getAutocompleteResponse(text, caretpos, skipVejnavn, adgangsadresseid, this.options.supplerendebynavn, this.options.stormodtagerpostnumre).then(function (result) {
	          return _this._handleResponse(request, result);
	        }, function (error) {
	          return _this._handleFailedRequest(request, error);
	        });
	      } else {
	        this._handleResponse(request, []);
	      }
	    }
	  }, {
	    key: "_handleFailedRequest",
	    value: function _handleFailedRequest(request, error) {
	      var _this2 = this;

	      console.error('DAWA request failed', error);
	      return delay(this.options.retryDelay).then(function () {
	        if (!_this2.state.pendingRequest) {
	          _this2._scheduleRequest(request);
	        }

	        _this2._requestCompleted();
	      });
	    }
	  }, {
	    key: "_handleResponse",
	    value: function _handleResponse(request, result) {
	      if (request.selected) {
	        if (result.length === 1) {
	          var item = result[0];

	          if (item.type === this.options.type) {
	            this.options.selectCallback(item);
	          } else {
	            if (!this.state.pendingRequest) {
	              this.state.pendingRequest = {
	                selected: item
	              };
	            }
	          }
	        } else if (this.options.renderCallback) {
	          this.options.renderCallback(result);
	        }
	      } else if (request.selectedId) {
	        if (result.length === 1) {
	          this.selected = result[0];
	          this.options.initialRenderCallback(result[0].tekst);
	        }
	      } else {
	        if (this.options.renderCallback) {
	          this.options.renderCallback(result);
	        }
	      }

	      this._requestCompleted();
	    }
	  }, {
	    key: "_requestCompleted",
	    value: function _requestCompleted() {
	      this.state.currentRequest = this.state.pendingRequest;
	      this.state.pendingRequest = null;

	      if (this.state.currentRequest) {
	        this._executeRequest();
	      }
	    }
	  }, {
	    key: "setRenderCallback",
	    value: function setRenderCallback(renderCallback) {
	      this.options.renderCallback = renderCallback;
	    }
	  }, {
	    key: "setInitialRenderCallback",
	    value: function setInitialRenderCallback(renderCallback) {
	      this.options.initialRenderCallback = renderCallback;
	    }
	  }, {
	    key: "setSelectCallback",
	    value: function setSelectCallback(selectCallback) {
	      this.options.selectCallback = selectCallback;
	    }
	  }, {
	    key: "update",
	    value: function update(text, caretpos) {
	      var request = {
	        text: text,
	        caretpos: caretpos
	      };

	      this._scheduleRequest(request);
	    }
	  }, {
	    key: "select",
	    value: function select(item) {
	      var request = {
	        selected: item
	      };

	      this._scheduleRequest(request);
	    }
	  }, {
	    key: "selectInitial",
	    value: function selectInitial(id) {
	      var request = {
	        selectedId: id
	      };

	      this._scheduleRequest(request);
	    }
	  }, {
	    key: "destroy",
	    value: function destroy() {}
	  }]);

	  return AutocompleteController;
	}();

	function dawaAutocomplete(inputElm, options) {
	  options = Object.assign({
	    select: function select() {
	      return null;
	    }
	  }, options);
	  var controllerOptions = ['baseUrl', 'minLength', 'params', 'fuzzy', 'stormodtagerpostnumre', 'supplerendebynavn', 'type'].reduce(function (memo, optionName) {
	    if (options.hasOwnProperty(optionName)) {
	      memo[optionName] = options[optionName];
	    }

	    return memo;
	  }, {});

	  if (!controllerOptions.type) {
	    if (options.adgangsadresserOnly) {
	      controllerOptions.type = 'adgangsadresse';
	    } else {
	      controllerOptions.type = 'adresse';
	    }
	  }

	  var controller = new AutocompleteController(controllerOptions);
	  var ui = autocompleteUi(inputElm, {
	    onSelect: function onSelect(suggestion) {
	      controller.select(suggestion);
	    },
	    onTextChange: function onTextChange(newText, newCaretpos) {
	      controller.update(newText, newCaretpos);
	    },
	    render: options.render,
	    multiline: options.multiline || false
	  });
	  controller.setRenderCallback(function (suggestions) {
	    return ui.setSuggestions(suggestions);
	  });
	  controller.setSelectCallback(function (selected) {
	    ui.selectAndClose(selected.tekst);
	    options.select(selected);
	  });
	  controller.setInitialRenderCallback(function (text) {
	    return ui.selectAndClose(text);
	  });

	  if (options.id) {
	    controller.selectInitial(options.id);
	  }

	  return {
	    id: function id(_id) {
	      return controller.selectInitial(_id);
	    },
	    destroy: function destroy() {
	      return ui.destroy();
	    },
	    selected: function selected() {
	      return controller.selected;
	    }
	  };
	}

	exports.dawaAutocomplete = dawaAutocomplete;

	Object.defineProperty(exports, '__esModule', { value: true });

}));
