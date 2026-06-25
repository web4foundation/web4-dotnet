class Web4Keyholes {
  #bridge = new WebSocketBridge();

  set(key, node, booleanAttributeName) {
    this[key] = new Web4Keyhole(this.#bridge, key, node);
    if (booleanAttributeName)
      this[key].booleanAttributeName = booleanAttributeName;
    else
      delete this.booleanAttributeName;
  }

  get nodes() {
    return Object.fromEntries(Object
      .values(keyholes)
      .map(k => [k.key, k.node]
    ));
  }

  getElementByKey(key) {
    return this[key]?.node;
  }

  dump() {
    this.#bridge.dump();
  }

  ping(repeat) {
    repeat = repeat ?? 1;
    console.time("ping");
    this.#bridge.ping().then(() => {
      console.timeEnd("ping");
      if (--repeat > 0)
        this.ping(repeat);
    });
  }
}

class Web4Keyhole {
  #bridge;
  key;
  node;
  static #transitionBatch = {
    mutations: [],
    nodes: [],
    keyholesInvalidated: false,
    concurrent: 0
  };

  static {
    document.addEventListener('DOMContentLoaded', () => document.registerKeyholes());
  }

  constructor(bridge, key, node) {
    this.#bridge = bridge;
    this.key = key;
    this.node = node;
  }

  setValue(value) {
    if (this.node.nodeType === Node.TEXT_NODE)
      this.node.nodeValue = value;
    else if (this.node.nodeType === Node.ATTRIBUTE_NODE && !this.booleanAttributeName)
      this.node.value = value;
    else
      this.node[this.booleanAttributeName] = value;
  }

  setNode(rawHtml, viewTransitionNameNew, viewTransitionNameOld) {
    let oldNode = this.node;
    let newNode = this.#createNode(rawHtml)
    newNode.registerKeyholes(this.key);

    let mutation = () => oldNode.replaceWith(newNode);

    if (!this.node.shouldAnimate(viewTransitionNameNew)) {
      mutation();
    } else {
      this.#prepareNode(newNode, viewTransitionNameNew);
      this.#prepareNode(oldNode, viewTransitionNameOld ?? viewTransitionNameNew);
      this.#prepareSiblings();
      this.#batchMutation(mutation, true);
    }
  }

  pushNode(rawHtml, key, viewTransitionName) {
    let newNode = this.#createNode(rawHtml);
    newNode.registerKeyholes(key);

    let mutation = () => this.node.before(newNode);

    if (!this.node.shouldAnimate(viewTransitionName)) {
      mutation();
    } else {
      this.#prepareNode(newNode, viewTransitionName);
      this.#prepareSiblings();
      this.#batchMutation(mutation, false);
    }
  }

  popNode(viewTransitionName) {
    let mutation = () => this.node.parentNode.removeChild(this.node);

    if (!this.node.shouldAnimate(viewTransitionName)) {
      mutation();
    } else {
      this.#prepareNode(this.node, viewTransitionName);
      this.#prepareSiblings();
      this.#batchMutation(mutation, true);
    }
  }

  dispatchEvent(event, trim) {
    this.#bridge.dispatchEvent(event, trim, this.key);
  }

  #batchMutation(mutation, invalidatesKeyholes) {
    Web4Keyhole.#transitionBatch.mutations.push(mutation);

    if (invalidatesKeyholes)
      Web4Keyhole.#transitionBatch.keyholesInvalidated = true;
  }

  #createNode(rawHtml) {
    if (!rawHtml)
      return document.createTextNode("");

    let fragment = document.createRange().createContextualFragment(rawHtml);
    return fragment.children[0] ?? fragment.childNodes[0];
  }

  #prepareNode(node, viewTransitionName) {
    if (!Web4Keyhole.#transitionBatch.nodes.includes(node))
      Web4Keyhole.#transitionBatch.nodes.push(node);

    if (node.style)
      node.style.viewTransitionName = viewTransitionName.replaceAll(":", "-");
  }

  #prepareSiblings() {
    this.node.parentElement.childNodes.forEach((sibling, i) => {
      if (!Web4Keyhole.#transitionBatch.nodes.includes(sibling))
        Web4Keyhole.#transitionBatch.nodes.push(sibling);
      
      if (sibling.style && !sibling.style.viewTransitionName)
        sibling.style.viewTransitionName = `web4-sibling-${this.key.replaceAll(":", "-")}-${i}`;
    });
  }

  static flushTransitionBatch() {
    let mutations = Web4Keyhole.#transitionBatch.mutations;
    let nodes = Web4Keyhole.#transitionBatch.nodes;
    let keyholesInvalidated = Web4Keyhole.#transitionBatch.keyholesInvalidated;

    if (!document.startViewTransition || mutations.length == 0) {
      if (keyholesInvalidated)
        document.unregisterKeyholes();
      return;
    }

    Web4Keyhole.#transitionBatch.mutations = [];
    Web4Keyhole.#transitionBatch.nodes = [];
    Web4Keyhole.#transitionBatch.keyholesInvalidated = false;
    Web4Keyhole.#transitionBatch.concurrent++;

    document.startViewTransition(() => {
      mutations.forEach(mutation => mutation());
    })
    .finished.then(() => {
      nodes.forEach(node => {
        if (node.style?.viewTransitionName?.startsWith("web4")) {
          node.style.removeProperty("view-transition-name");
          if (node.style.length == 0)
            node.removeAttribute("style");
        }
      });
      if (--Web4Keyhole.#transitionBatch.concurrent == 0 && keyholesInvalidated)
        document.unregisterKeyholes();
    });
  }
}

class WebSocketBridge {
  #webSocket = new WebSocket(location.pathname.endsWith('/') ? `app` : `${location.pathname}/app`);
  #messageID = 0;
  #reconnectionAttempts = 0;
  #promises = new Map();

  constructor() {
    this.#webSocket.onmessage = e => this.#handleMessage(e);
    this.#webSocket.onerror = e => console.error(e);
    this.#webSocket.onopen = e => this.#updateConnectionState(this.#webSocket, e);
    this.#webSocket.onclose = e => this.#updateConnectionState(this.#webSocket, e);
    setTimeout(e => this.#updateConnectionState(this.#webSocket, e), 1000);
    window.addEventListener("online", e => this.#updateConnectionState(this.#webSocket, e));
    window.addEventListener("visibilitychange", e => this.#updateConnectionState(this.#webSocket, e));
    window.onbeforeunload = e => this.#webSocket = undefined;
  }

  dispatchEvent(event, trim, key) {
    if (trim?.includes("preventDefault"))
      this.preventDefault();

    let { propagationID, propagationLevel } = this.#setPropagation(event);
    
    let params = [
      event.trim(trim), 
      propagationID
    ];

    if (propagationLevel)
      params.push(propagationLevel)

    let message = {
      jsonrpc: "2.0", 
      method: `keyholes['${key}'].dispatchEvent`, 
      params: params
    };
    this.#webSocket.send(JSON.stringify(message));
  }
  
  ping() {
    let message = { 
      jsonrpc: "2.0", 
      method: "keyholes.ping", 
      id: ++this.#messageID,
    };
    this.#webSocket.send(JSON.stringify(message));

    return new Promise((resolve, reject) => {
      this.#promises.set(this.#messageID, { resolve, reject });
    });
  }

  dump() {
    let message = {
      jsonrpc: "2.0", 
      method: "keyholes.dump", 
    };
    this.#webSocket.send(JSON.stringify(message));
  }

  #setPropagation(event) {
    if (!event.propagationID) {
      event.propagationID = ++Event.prototype.NEXT_PROPAGATION_ID;
      event.currentTarget.propagationID = event.propagationID;
      Event.prototype.NEXT_PROPAGATION_LEVEL = 0;
    }

    if (event.currentTarget.propagationID != event.propagationID) {
      event.currentTarget.propagationID = event.propagationID;
      Event.prototype.NEXT_PROPAGATION_LEVEL++;
    }

    return { 
      propagationID: Event.prototype.NEXT_PROPAGATION_ID, 
      propagationLevel: Event.prototype.NEXT_PROPAGATION_LEVEL,
    };
  }

  #handleMessage(event) {
    let batch = JSON.parse(event.data);
    batch = Array.isArray(batch) ? batch : [batch];
    this.#unescapeParams(batch);
    this.#fixDuplicates(batch);
    batch.forEach(rpc => {

      // A function to call
      if (rpc.method) {
        let [obj, func] = rpc.method
          .split(/[.\[\]\'\"]/)
          .filter(i => i)
          .reduce(([obj, func], prop) => [func, func[prop]], [null, window]);
        func.apply(obj, rpc.params);
      }

      // A result to return
      if (rpc.id) {
        let promise = this.#promises.get(rpc.id);
        this.#promises.delete(rpc.id);
        if (rpc.error)
          promise.reject(rpc.error);
        else
          promise.resolve(rpc.result);
      }
    });

    Web4Keyhole.flushTransitionBatch();
  }

  #unescapeParams(batch) {
    batch.forEach(rpc => {
      if (rpc.params) {
        rpc.params.forEach((param, i) => {
          if (typeof param === 'string' && param.startsWith("%o")) {
            rpc.params[i] = param
              .substring(2)
              .split(/[.\[\]\'\"]/)
              .filter(i => i)
              .reduce((obj, prop) => obj != undefined ? obj[prop] : undefined, globalThis);
          }
        });
      }
    });
  }

  #fixDuplicates(batch) {
    let duplicatesNew = {};
    let duplicatesOld = {};

    batch.forEach(rpc => {
      let iNew = -1, iOld = -1;
      if (rpc.method?.endsWith("setNode")) { iNew = 1; iOld = 2;}
      else if (rpc.method?.endsWith("popNode")) { iOld = 0; } 
      else if (rpc.method?.endsWith("pushNode")) { iNew = 2; }
      else { return; }

      if (iNew >= 0) {
        let name = rpc.params[iNew];
        let occurrances = duplicatesNew[name] ?? 0;
        duplicatesNew[name] = ++occurrances;
        if (occurrances > 1)
          rpc.params[iNew] = `${name}-${occurrances}`
      }

      if (iOld >= 0) {
        let name = rpc.params[iOld];
        let occurrances = duplicatesOld[name] ?? 0;
        duplicatesOld[name] = ++occurrances;
        if (occurrances > 1)
          rpc.params[iOld] = `${name}-${occurrances}`
      }
    });
  }

  async #updateConnectionState(e) {
    switch (this.#webSocket?.readyState) {
      case WebSocket.CONNECTING:
        this.#getOrCreateModal().showModal();
        break;
      case WebSocket.OPEN:
        document.getElementById("web4Modal")?.close();
        break;
      case WebSocket.CLOSING:
      case WebSocket.CLOSED:
        this.#getOrCreateModal().showModal();
        document.getElementById("web4ModalMessage").textContent = e.reason ? e.reason : "Looking for server...";
        if (this.#reconnectionAttempts == 0) {
          while (++this.#reconnectionAttempts <= 10) {
            console.debug(`Web4 reconnect: (attempt ${this.#reconnectionAttempts} of 10)...`);
            new WebSocket(`/_app/alive`)
              .onopen = e => location.reload();
            await new Promise(resolve => setTimeout(resolve, 1000));
          }
          this.#reconnectionAttempts = 0;
        }
        break;
    }
  }
  
  #getOrCreateModal() {
    let web4Modal = document.getElementById("web4Modal");
    if (!web4Modal) {
      const WEB4_MODAL_TEMPLATE = `
        <dialog id="web4Modal" onkeydown="event.preventDefault()">
          <div>
            <progress></progress>
            <div id="web4ModalMessage">Looking for server...</div>
          </div>
        </dialog>
        <style>
          #web4Modal::backdrop { background-color: #ffffff66; backdrop-filter: grayscale(0.75) blur(6px); }
          #web4Modal:focus { outline: none; }
          #web4Modal[open] { 
            background-color: transparent; 
            border-width: 0;
            animation: .5s cubic-bezier(0.165, 0.840, 0.440, 1.000) forwards zoom-fade-in;
          }
        </style>
      `;
      document.body.insertAdjacentHTML('beforeend', WEB4_MODAL_TEMPLATE);
      web4Modal = document.getElementById("web4Modal")
    }
    return web4Modal;
  }
}

this.keyholes = new Web4Keyholes();

Event.prototype.trim = function(members) {
  if (!members)
    return {};

  if (members === '*')
    members = Event.prototype.SERIALIZABLE_MEMBERS;
  else if (typeof members === "string")
    members = members.split(",");

  let trimmed = {};
  
  members.forEach(member => {
    member = member.trim();
    if (member in this) {
      if (this[member] instanceof EventTarget) {
        let eventTarget = {};
        if (this[member].id) eventTarget.id = this[member].id;
        if (this[member].name) eventTarget.name = this[member].name;
        if (this[member].value) eventTarget.value = this[member].value;
        if (this[member].value === "on") eventTarget.checked = this[member].checked;
        if (Object.keys(eventTarget).length > 0)
          trimmed[member] = eventTarget;
      } else {
        trimmed[member] = this[member];
      }
    }
  });

  return trimmed;
}

Event.prototype.NEXT_PROPAGATION_ID = 0;
Event.prototype.NEXT_PROPAGATION_LEVEL = 0;
Event.prototype.SERIALIZABLE_MEMBERS = [ "absolute", "acceleration", "accelerationIncludingGravity", "alpha", "altitudeAngle", "altKey", "animationName", "azimuthAngle", "beta", "bubbles", "button", "buttons", "cancelable", "changedTouches", "clientX", "clientY", "code", "colNo", "composed", "ctrlKey", "currentTarget", "data", "dataTransfer", "defaultPrevented", "deltaMode", "deltaX", "deltaY", "deltaZ", "detail", "elapsedTime", "error", "eventPhase", "fileName", "gamma", "height", "inputType", "interval", "isComposing", "isPrimary", "isTrusted", "key", "length", "lengthComputable", "lineNo", "loaded", "location", "message", "metaKey", "movementX", "movementY", "newState", "newUrl", "offsetX", "offsetY", "oldState", "oldUrl", "pageX", "pageY", "persisted", "pointerID", "pointerType", "pressure", "propertyName", "pseudoElement", "relatedTarget", "repeat", "rotationRate", "screenX", "screenY", "shiftKey", "skipped", "submitter", "tangentialPressure", "target", "targetTouches", "timeStamp", "tiltX", "tiltY", "total", "touches", "twist", "type", "width", "x", "y", "id", "name", "value", "checked" ];

Node.prototype.shouldAnimate = function(viewTransitionName) {
  // TODO: Not sure about BODY tag.  Perhaps better to verify by CSS props instead?
  let isInAnimationContainer =  ["ROW", "COL", "LIST", "BODY"].includes(this.parentElement?.tagName);
  return document.startViewTransition && viewTransitionName && isInAnimationContainer;
}

Node.prototype.registerKeyholes = function(key) {
  if (key)
    keyholes.set(key, this);

  let comments = document.evaluate('//comment()', this, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
  for (let i = comments.snapshotLength - 1; i >= 0; i--) {
    let comment = comments.snapshotItem(i);
    let key = comment.textContent;
    if (key.startsWith('key:')) {
      if (key.endsWith('/')) {
        // Needed as a placeholder for zero-length iterators, do not removeChild
        key = key.replace('key:', '').replace(' /', '');
        keyholes.set(key, comment);
      } else {
        comment.parentElement?.removeChild(comment);
      }
    } else if (key.startsWith('/key:')) {
      key = key.replace('/key:', '');
      let node = comment.previousSibling;
      if (node.nodeType === Node.COMMENT_NODE) {
        // Text node was missing because keyhole value was an empty string (""), e.g. `<!--key:123--><!--/key:123-->`
        node = node.parentNode.insertBefore(document.createTextNode(""), comment);
      }
      keyholes.set(key, node);
      comment.parentElement?.removeChild(comment);
    }
  }
  
  let attrs = document.evaluate('//*/attribute::*', this, null, XPathResult.UNORDERED_NODE_SNAPSHOT_TYPE, null);
  for (let i = 0; i < attrs.snapshotLength; i++) {
    let attr = attrs.snapshotItem(i);
    let key = attr.name;
    if (key.startsWith('key:')) {
      key = key.replace('key:', '');
      if (attr.value === "") {
        let node = attrs.snapshotItem(i - 1)
        keyholes.set(key, node);
      } else {
        keyholes.set(key, attr.ownerElement, attr.value);
      }
      attr.ownerElement.removeAttribute(attr.name)
    }
  }
}

HTMLDocument.prototype.unregisterKeyholes = function() {
  for (let key in keyholes) {
    let node = keyholes[key].node;
    if (node.nodeType == Node.ATTRIBUTE_NODE)
      node = node.ownerElement;
    if (node instanceof Node && node != document && !document.body.contains(node))
      delete keyholes[key];
  }
}