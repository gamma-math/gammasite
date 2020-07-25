function unwrapExports (x) {
	return x && x.__esModule && Object.prototype.hasOwnProperty.call(x, 'default') ? x['default'] : x;
}

function createCommonjsModule(fn, module) {
	return module = { exports: {} }, fn(module, module.exports), module.exports;
}

var _global = createCommonjsModule(function (module) {
// https://github.com/zloirock/core-js/issues/86#issuecomment-115759028
var global = module.exports = typeof window != 'undefined' && window.Math == Math
  ? window : typeof self != 'undefined' && self.Math == Math ? self
  // eslint-disable-next-line no-new-func
  : Function('return this')();
if (typeof __g == 'number') __g = global; // eslint-disable-line no-undef
});

var _core = createCommonjsModule(function (module) {
var core = module.exports = { version: '2.6.5' };
if (typeof __e == 'number') __e = core; // eslint-disable-line no-undef
});
var _core_1 = _core.version;

var _isObject = function (it) {
  return typeof it === 'object' ? it !== null : typeof it === 'function';
};

var _anObject = function (it) {
  if (!_isObject(it)) throw TypeError(it + ' is not an object!');
  return it;
};

var _fails = function (exec) {
  try {
    return !!exec();
  } catch (e) {
    return true;
  }
};

// Thank's IE8 for his funny defineProperty
var _descriptors = !_fails(function () {
  return Object.defineProperty({}, 'a', { get: function () { return 7; } }).a != 7;
});

var document$1 = _global.document;
// typeof document.createElement is 'object' in old IE
var is = _isObject(document$1) && _isObject(document$1.createElement);
var _domCreate = function (it) {
  return is ? document$1.createElement(it) : {};
};

var _ie8DomDefine = !_descriptors && !_fails(function () {
  return Object.defineProperty(_domCreate('div'), 'a', { get: function () { return 7; } }).a != 7;
});

// 7.1.1 ToPrimitive(input [, PreferredType])

// instead of the ES6 spec version, we didn't implement @@toPrimitive case
// and the second argument - flag - preferred type is a string
var _toPrimitive = function (it, S) {
  if (!_isObject(it)) return it;
  var fn, val;
  if (S && typeof (fn = it.toString) == 'function' && !_isObject(val = fn.call(it))) return val;
  if (typeof (fn = it.valueOf) == 'function' && !_isObject(val = fn.call(it))) return val;
  if (!S && typeof (fn = it.toString) == 'function' && !_isObject(val = fn.call(it))) return val;
  throw TypeError("Can't convert object to primitive value");
};

var dP = Object.defineProperty;

var f = _descriptors ? Object.defineProperty : function defineProperty(O, P, Attributes) {
  _anObject(O);
  P = _toPrimitive(P, true);
  _anObject(Attributes);
  if (_ie8DomDefine) try {
    return dP(O, P, Attributes);
  } catch (e) { /* empty */ }
  if ('get' in Attributes || 'set' in Attributes) throw TypeError('Accessors not supported!');
  if ('value' in Attributes) O[P] = Attributes.value;
  return O;
};

var _objectDp = {
	f: f
};

var _propertyDesc = function (bitmap, value) {
  return {
    enumerable: !(bitmap & 1),
    configurable: !(bitmap & 2),
    writable: !(bitmap & 4),
    value: value
  };
};

var _hide = _descriptors ? function (object, key, value) {
  return _objectDp.f(object, key, _propertyDesc(1, value));
} : function (object, key, value) {
  object[key] = value;
  return object;
};

var hasOwnProperty = {}.hasOwnProperty;
var _has = function (it, key) {
  return hasOwnProperty.call(it, key);
};

var id = 0;
var px = Math.random();
var _uid = function (key) {
  return 'Symbol('.concat(key === undefined ? '' : key, ')_', (++id + px).toString(36));
};

var _shared = createCommonjsModule(function (module) {
var SHARED = '__core-js_shared__';
var store = _global[SHARED] || (_global[SHARED] = {});

(module.exports = function (key, value) {
  return store[key] || (store[key] = value !== undefined ? value : {});
})('versions', []).push({
  version: _core.version,
  mode: 'global',
  copyright: '© 2019 Denis Pushkarev (zloirock.ru)'
});
});

var _functionToString = _shared('native-function-to-string', Function.toString);

var _redefine = createCommonjsModule(function (module) {
var SRC = _uid('src');

var TO_STRING = 'toString';
var TPL = ('' + _functionToString).split(TO_STRING);

_core.inspectSource = function (it) {
  return _functionToString.call(it);
};

(module.exports = function (O, key, val, safe) {
  var isFunction = typeof val == 'function';
  if (isFunction) _has(val, 'name') || _hide(val, 'name', key);
  if (O[key] === val) return;
  if (isFunction) _has(val, SRC) || _hide(val, SRC, O[key] ? '' + O[key] : TPL.join(String(key)));
  if (O === _global) {
    O[key] = val;
  } else if (!safe) {
    delete O[key];
    _hide(O, key, val);
  } else if (O[key]) {
    O[key] = val;
  } else {
    _hide(O, key, val);
  }
// add fake Function#toString for correct work wrapped methods / constructors with methods like LoDash isNative
})(Function.prototype, TO_STRING, function toString() {
  return typeof this == 'function' && this[SRC] || _functionToString.call(this);
});
});

var _aFunction = function (it) {
  if (typeof it != 'function') throw TypeError(it + ' is not a function!');
  return it;
};

// optional / simple context binding

var _ctx = function (fn, that, length) {
  _aFunction(fn);
  if (that === undefined) return fn;
  switch (length) {
    case 1: return function (a) {
      return fn.call(that, a);
    };
    case 2: return function (a, b) {
      return fn.call(that, a, b);
    };
    case 3: return function (a, b, c) {
      return fn.call(that, a, b, c);
    };
  }
  return function (/* ...args */) {
    return fn.apply(that, arguments);
  };
};

var PROTOTYPE = 'prototype';

var $export = function (type, name, source) {
  var IS_FORCED = type & $export.F;
  var IS_GLOBAL = type & $export.G;
  var IS_STATIC = type & $export.S;
  var IS_PROTO = type & $export.P;
  var IS_BIND = type & $export.B;
  var target = IS_GLOBAL ? _global : IS_STATIC ? _global[name] || (_global[name] = {}) : (_global[name] || {})[PROTOTYPE];
  var exports = IS_GLOBAL ? _core : _core[name] || (_core[name] = {});
  var expProto = exports[PROTOTYPE] || (exports[PROTOTYPE] = {});
  var key, own, out, exp;
  if (IS_GLOBAL) source = name;
  for (key in source) {
    // contains in native
    own = !IS_FORCED && target && target[key] !== undefined;
    // export native or passed
    out = (own ? target : source)[key];
    // bind timers to global for call from export context
    exp = IS_BIND && own ? _ctx(out, _global) : IS_PROTO && typeof out == 'function' ? _ctx(Function.call, out) : out;
    // extend global
    if (target) _redefine(target, key, out, type & $export.U);
    // export
    if (exports[key] != out) _hide(exports, key, exp);
    if (IS_PROTO && expProto[key] != out) expProto[key] = out;
  }
};
_global.core = _core;
// type bitmap
$export.F = 1;   // forced
$export.G = 2;   // global
$export.S = 4;   // static
$export.P = 8;   // proto
$export.B = 16;  // bind
$export.W = 32;  // wrap
$export.U = 64;  // safe
$export.R = 128; // real proto method for `library`
var _export = $export;

var toString = {}.toString;

var _cof = function (it) {
  return toString.call(it).slice(8, -1);
};

// fallback for non-array-like ES3 and non-enumerable old V8 strings

// eslint-disable-next-line no-prototype-builtins
var _iobject = Object('z').propertyIsEnumerable(0) ? Object : function (it) {
  return _cof(it) == 'String' ? it.split('') : Object(it);
};

// 7.2.1 RequireObjectCoercible(argument)
var _defined = function (it) {
  if (it == undefined) throw TypeError("Can't call method on  " + it);
  return it;
};

// to indexed object, toObject with fallback for non-array-like ES3 strings


var _toIobject = function (it) {
  return _iobject(_defined(it));
};

// 7.1.4 ToInteger
var ceil = Math.ceil;
var floor = Math.floor;
var _toInteger = function (it) {
  return isNaN(it = +it) ? 0 : (it > 0 ? floor : ceil)(it);
};

// 7.1.15 ToLength

var min = Math.min;
var _toLength = function (it) {
  return it > 0 ? min(_toInteger(it), 0x1fffffffffffff) : 0; // pow(2, 53) - 1 == 9007199254740991
};

var max = Math.max;
var min$1 = Math.min;
var _toAbsoluteIndex = function (index, length) {
  index = _toInteger(index);
  return index < 0 ? max(index + length, 0) : min$1(index, length);
};

// false -> Array#indexOf
// true  -> Array#includes



var _arrayIncludes = function (IS_INCLUDES) {
  return function ($this, el, fromIndex) {
    var O = _toIobject($this);
    var length = _toLength(O.length);
    var index = _toAbsoluteIndex(fromIndex, length);
    var value;
    // Array#includes uses SameValueZero equality algorithm
    // eslint-disable-next-line no-self-compare
    if (IS_INCLUDES && el != el) while (length > index) {
      value = O[index++];
      // eslint-disable-next-line no-self-compare
      if (value != value) return true;
    // Array#indexOf ignores holes, Array#includes - not
    } else for (;length > index; index++) if (IS_INCLUDES || index in O) {
      if (O[index] === el) return IS_INCLUDES || index || 0;
    } return !IS_INCLUDES && -1;
  };
};

var shared = _shared('keys');

var _sharedKey = function (key) {
  return shared[key] || (shared[key] = _uid(key));
};

var arrayIndexOf = _arrayIncludes(false);
var IE_PROTO = _sharedKey('IE_PROTO');

var _objectKeysInternal = function (object, names) {
  var O = _toIobject(object);
  var i = 0;
  var result = [];
  var key;
  for (key in O) if (key != IE_PROTO) _has(O, key) && result.push(key);
  // Don't enum bug & hidden keys
  while (names.length > i) if (_has(O, key = names[i++])) {
    ~arrayIndexOf(result, key) || result.push(key);
  }
  return result;
};

// IE 8- don't enum bug keys
var _enumBugKeys = (
  'constructor,hasOwnProperty,isPrototypeOf,propertyIsEnumerable,toLocaleString,toString,valueOf'
).split(',');

// 19.1.2.14 / 15.2.3.14 Object.keys(O)



var _objectKeys = Object.keys || function keys(O) {
  return _objectKeysInternal(O, _enumBugKeys);
};

var f$1 = Object.getOwnPropertySymbols;

var _objectGops = {
	f: f$1
};

var f$2 = {}.propertyIsEnumerable;

var _objectPie = {
	f: f$2
};

// 7.1.13 ToObject(argument)

var _toObject = function (it) {
  return Object(_defined(it));
};

// 19.1.2.1 Object.assign(target, source, ...)





var $assign = Object.assign;

// should work with symbols and should have deterministic property order (V8 bug)
var _objectAssign = !$assign || _fails(function () {
  var A = {};
  var B = {};
  // eslint-disable-next-line no-undef
  var S = Symbol();
  var K = 'abcdefghijklmnopqrst';
  A[S] = 7;
  K.split('').forEach(function (k) { B[k] = k; });
  return $assign({}, A)[S] != 7 || Object.keys($assign({}, B)).join('') != K;
}) ? function assign(target, source) { // eslint-disable-line no-unused-vars
  var T = _toObject(target);
  var aLen = arguments.length;
  var index = 1;
  var getSymbols = _objectGops.f;
  var isEnum = _objectPie.f;
  while (aLen > index) {
    var S = _iobject(arguments[index++]);
    var keys = getSymbols ? _objectKeys(S).concat(getSymbols(S)) : _objectKeys(S);
    var length = keys.length;
    var j = 0;
    var key;
    while (length > j) if (isEnum.call(S, key = keys[j++])) T[key] = S[key];
  } return T;
} : $assign;

// 19.1.3.1 Object.assign(target, source)


_export(_export.S + _export.F, 'Object', { assign: _objectAssign });

// true  -> String#at
// false -> String#codePointAt
var _stringAt = function (TO_STRING) {
  return function (that, pos) {
    var s = String(_defined(that));
    var i = _toInteger(pos);
    var l = s.length;
    var a, b;
    if (i < 0 || i >= l) return TO_STRING ? '' : undefined;
    a = s.charCodeAt(i);
    return a < 0xd800 || a > 0xdbff || i + 1 === l || (b = s.charCodeAt(i + 1)) < 0xdc00 || b > 0xdfff
      ? TO_STRING ? s.charAt(i) : a
      : TO_STRING ? s.slice(i, i + 2) : (a - 0xd800 << 10) + (b - 0xdc00) + 0x10000;
  };
};

var at = _stringAt(true);

 // `AdvanceStringIndex` abstract operation
// https://tc39.github.io/ecma262/#sec-advancestringindex
var _advanceStringIndex = function (S, index, unicode) {
  return index + (unicode ? at(S, index).length : 1);
};

var _wks = createCommonjsModule(function (module) {
var store = _shared('wks');

var Symbol = _global.Symbol;
var USE_SYMBOL = typeof Symbol == 'function';

var $exports = module.exports = function (name) {
  return store[name] || (store[name] =
    USE_SYMBOL && Symbol[name] || (USE_SYMBOL ? Symbol : _uid)('Symbol.' + name));
};

$exports.store = store;
});

// getting tag from 19.1.3.6 Object.prototype.toString()

var TAG = _wks('toStringTag');
// ES3 wrong here
var ARG = _cof(function () { return arguments; }()) == 'Arguments';

// fallback for IE11 Script Access Denied error
var tryGet = function (it, key) {
  try {
    return it[key];
  } catch (e) { /* empty */ }
};

var _classof = function (it) {
  var O, T, B;
  return it === undefined ? 'Undefined' : it === null ? 'Null'
    // @@toStringTag case
    : typeof (T = tryGet(O = Object(it), TAG)) == 'string' ? T
    // builtinTag case
    : ARG ? _cof(O)
    // ES3 arguments fallback
    : (B = _cof(O)) == 'Object' && typeof O.callee == 'function' ? 'Arguments' : B;
};

var builtinExec = RegExp.prototype.exec;

 // `RegExpExec` abstract operation
// https://tc39.github.io/ecma262/#sec-regexpexec
var _regexpExecAbstract = function (R, S) {
  var exec = R.exec;
  if (typeof exec === 'function') {
    var result = exec.call(R, S);
    if (typeof result !== 'object') {
      throw new TypeError('RegExp exec method returned something other than an Object or null');
    }
    return result;
  }
  if (_classof(R) !== 'RegExp') {
    throw new TypeError('RegExp#exec called on incompatible receiver');
  }
  return builtinExec.call(R, S);
};

// 21.2.5.3 get RegExp.prototype.flags

var _flags = function () {
  var that = _anObject(this);
  var result = '';
  if (that.global) result += 'g';
  if (that.ignoreCase) result += 'i';
  if (that.multiline) result += 'm';
  if (that.unicode) result += 'u';
  if (that.sticky) result += 'y';
  return result;
};

var nativeExec = RegExp.prototype.exec;
// This always refers to the native implementation, because the
// String#replace polyfill uses ./fix-regexp-well-known-symbol-logic.js,
// which loads this file before patching the method.
var nativeReplace = String.prototype.replace;

var patchedExec = nativeExec;

var LAST_INDEX = 'lastIndex';

var UPDATES_LAST_INDEX_WRONG = (function () {
  var re1 = /a/,
      re2 = /b*/g;
  nativeExec.call(re1, 'a');
  nativeExec.call(re2, 'a');
  return re1[LAST_INDEX] !== 0 || re2[LAST_INDEX] !== 0;
})();

// nonparticipating capturing group, copied from es5-shim's String#split patch.
var NPCG_INCLUDED = /()??/.exec('')[1] !== undefined;

var PATCH = UPDATES_LAST_INDEX_WRONG || NPCG_INCLUDED;

if (PATCH) {
  patchedExec = function exec(str) {
    var re = this;
    var lastIndex, reCopy, match, i;

    if (NPCG_INCLUDED) {
      reCopy = new RegExp('^' + re.source + '$(?!\\s)', _flags.call(re));
    }
    if (UPDATES_LAST_INDEX_WRONG) lastIndex = re[LAST_INDEX];

    match = nativeExec.call(re, str);

    if (UPDATES_LAST_INDEX_WRONG && match) {
      re[LAST_INDEX] = re.global ? match.index + match[0].length : lastIndex;
    }
    if (NPCG_INCLUDED && match && match.length > 1) {
      // Fix browsers whose `exec` methods don't consistently return `undefined`
      // for NPCG, like IE8. NOTE: This doesn' work for /(.?)?/
      // eslint-disable-next-line no-loop-func
      nativeReplace.call(match[0], reCopy, function () {
        for (i = 1; i < arguments.length - 2; i++) {
          if (arguments[i] === undefined) match[i] = undefined;
        }
      });
    }

    return match;
  };
}

var _regexpExec = patchedExec;

_export({
  target: 'RegExp',
  proto: true,
  forced: _regexpExec !== /./.exec
}, {
  exec: _regexpExec
});

var SPECIES = _wks('species');

var REPLACE_SUPPORTS_NAMED_GROUPS = !_fails(function () {
  // #replace needs built-in support for named groups.
  // #match works fine because it just return the exec results, even if it has
  // a "grops" property.
  var re = /./;
  re.exec = function () {
    var result = [];
    result.groups = { a: '7' };
    return result;
  };
  return ''.replace(re, '$<a>') !== '7';
});

var SPLIT_WORKS_WITH_OVERWRITTEN_EXEC = (function () {
  // Chrome 51 has a buggy "split" implementation when RegExp#exec !== nativeExec
  var re = /(?:)/;
  var originalExec = re.exec;
  re.exec = function () { return originalExec.apply(this, arguments); };
  var result = 'ab'.split(re);
  return result.length === 2 && result[0] === 'a' && result[1] === 'b';
})();

var _fixReWks = function (KEY, length, exec) {
  var SYMBOL = _wks(KEY);

  var DELEGATES_TO_SYMBOL = !_fails(function () {
    // String methods call symbol-named RegEp methods
    var O = {};
    O[SYMBOL] = function () { return 7; };
    return ''[KEY](O) != 7;
  });

  var DELEGATES_TO_EXEC = DELEGATES_TO_SYMBOL ? !_fails(function () {
    // Symbol-named RegExp methods call .exec
    var execCalled = false;
    var re = /a/;
    re.exec = function () { execCalled = true; return null; };
    if (KEY === 'split') {
      // RegExp[@@split] doesn't call the regex's exec method, but first creates
      // a new one. We need to return the patched regex when creating the new one.
      re.constructor = {};
      re.constructor[SPECIES] = function () { return re; };
    }
    re[SYMBOL]('');
    return !execCalled;
  }) : undefined;

  if (
    !DELEGATES_TO_SYMBOL ||
    !DELEGATES_TO_EXEC ||
    (KEY === 'replace' && !REPLACE_SUPPORTS_NAMED_GROUPS) ||
    (KEY === 'split' && !SPLIT_WORKS_WITH_OVERWRITTEN_EXEC)
  ) {
    var nativeRegExpMethod = /./[SYMBOL];
    var fns = exec(
      _defined,
      SYMBOL,
      ''[KEY],
      function maybeCallNative(nativeMethod, regexp, str, arg2, forceStringMethod) {
        if (regexp.exec === _regexpExec) {
          if (DELEGATES_TO_SYMBOL && !forceStringMethod) {
            // The native String method already delegates to @@method (this
            // polyfilled function), leasing to infinite recursion.
            // We avoid it by directly calling the native @@method method.
            return { done: true, value: nativeRegExpMethod.call(regexp, str, arg2) };
          }
          return { done: true, value: nativeMethod.call(str, regexp, arg2) };
        }
        return { done: false };
      }
    );
    var strfn = fns[0];
    var rxfn = fns[1];

    _redefine(String.prototype, KEY, strfn);
    _hide(RegExp.prototype, SYMBOL, length == 2
      // 21.2.5.8 RegExp.prototype[@@replace](string, replaceValue)
      // 21.2.5.11 RegExp.prototype[@@split](string, limit)
      ? function (string, arg) { return rxfn.call(string, this, arg); }
      // 21.2.5.6 RegExp.prototype[@@match](string)
      // 21.2.5.9 RegExp.prototype[@@search](string)
      : function (string) { return rxfn.call(string, this); }
    );
  }
};

var max$1 = Math.max;
var min$2 = Math.min;
var floor$1 = Math.floor;
var SUBSTITUTION_SYMBOLS = /\$([$&`']|\d\d?|<[^>]*>)/g;
var SUBSTITUTION_SYMBOLS_NO_NAMED = /\$([$&`']|\d\d?)/g;

var maybeToString = function (it) {
  return it === undefined ? it : String(it);
};

// @@replace logic
_fixReWks('replace', 2, function (defined, REPLACE, $replace, maybeCallNative) {
  return [
    // `String.prototype.replace` method
    // https://tc39.github.io/ecma262/#sec-string.prototype.replace
    function replace(searchValue, replaceValue) {
      var O = defined(this);
      var fn = searchValue == undefined ? undefined : searchValue[REPLACE];
      return fn !== undefined
        ? fn.call(searchValue, O, replaceValue)
        : $replace.call(String(O), searchValue, replaceValue);
    },
    // `RegExp.prototype[@@replace]` method
    // https://tc39.github.io/ecma262/#sec-regexp.prototype-@@replace
    function (regexp, replaceValue) {
      var res = maybeCallNative($replace, regexp, this, replaceValue);
      if (res.done) return res.value;

      var rx = _anObject(regexp);
      var S = String(this);
      var functionalReplace = typeof replaceValue === 'function';
      if (!functionalReplace) replaceValue = String(replaceValue);
      var global = rx.global;
      if (global) {
        var fullUnicode = rx.unicode;
        rx.lastIndex = 0;
      }
      var results = [];
      while (true) {
        var result = _regexpExecAbstract(rx, S);
        if (result === null) break;
        results.push(result);
        if (!global) break;
        var matchStr = String(result[0]);
        if (matchStr === '') rx.lastIndex = _advanceStringIndex(S, _toLength(rx.lastIndex), fullUnicode);
      }
      var accumulatedResult = '';
      var nextSourcePosition = 0;
      for (var i = 0; i < results.length; i++) {
        result = results[i];
        var matched = String(result[0]);
        var position = max$1(min$2(_toInteger(result.index), S.length), 0);
        var captures = [];
        // NOTE: This is equivalent to
        //   captures = result.slice(1).map(maybeToString)
        // but for some reason `nativeSlice.call(result, 1, result.length)` (called in
        // the slice polyfill when slicing native arrays) "doesn't work" in safari 9 and
        // causes a crash (https://pastebin.com/N21QzeQA) when trying to debug it.
        for (var j = 1; j < result.length; j++) captures.push(maybeToString(result[j]));
        var namedCaptures = result.groups;
        if (functionalReplace) {
          var replacerArgs = [matched].concat(captures, position, S);
          if (namedCaptures !== undefined) replacerArgs.push(namedCaptures);
          var replacement = String(replaceValue.apply(undefined, replacerArgs));
        } else {
          replacement = getSubstitution(matched, S, position, captures, namedCaptures, replaceValue);
        }
        if (position >= nextSourcePosition) {
          accumulatedResult += S.slice(nextSourcePosition, position) + replacement;
          nextSourcePosition = position + matched.length;
        }
      }
      return accumulatedResult + S.slice(nextSourcePosition);
    }
  ];

    // https://tc39.github.io/ecma262/#sec-getsubstitution
  function getSubstitution(matched, str, position, captures, namedCaptures, replacement) {
    var tailPos = position + matched.length;
    var m = captures.length;
    var symbols = SUBSTITUTION_SYMBOLS_NO_NAMED;
    if (namedCaptures !== undefined) {
      namedCaptures = _toObject(namedCaptures);
      symbols = SUBSTITUTION_SYMBOLS;
    }
    return $replace.call(replacement, symbols, function (match, ch) {
      var capture;
      switch (ch.charAt(0)) {
        case '$': return '$';
        case '&': return matched;
        case '`': return str.slice(0, position);
        case "'": return str.slice(tailPos);
        case '<':
          capture = namedCaptures[ch.slice(1, -1)];
          break;
        default: // \d\d?
          var n = +ch;
          if (n === 0) return match;
          if (n > m) {
            var f = floor$1(n / 10);
            if (f === 0) return match;
            if (f <= m) return captures[f - 1] === undefined ? ch.charAt(1) : captures[f - 1] + ch.charAt(1);
            return match;
          }
          capture = captures[n - 1];
      }
      return capture === undefined ? '' : capture;
    });
  }
});

// 7.2.8 IsRegExp(argument)


var MATCH = _wks('match');
var _isRegexp = function (it) {
  var isRegExp;
  return _isObject(it) && ((isRegExp = it[MATCH]) !== undefined ? !!isRegExp : _cof(it) == 'RegExp');
};

// 7.3.20 SpeciesConstructor(O, defaultConstructor)


var SPECIES$1 = _wks('species');
var _speciesConstructor = function (O, D) {
  var C = _anObject(O).constructor;
  var S;
  return C === undefined || (S = _anObject(C)[SPECIES$1]) == undefined ? D : _aFunction(S);
};

var $min = Math.min;
var $push = [].push;
var $SPLIT = 'split';
var LENGTH = 'length';
var LAST_INDEX$1 = 'lastIndex';
var MAX_UINT32 = 0xffffffff;

// babel-minify transpiles RegExp('x', 'y') -> /x/y and it causes SyntaxError
var SUPPORTS_Y = !_fails(function () { });

// @@split logic
_fixReWks('split', 2, function (defined, SPLIT, $split, maybeCallNative) {
  var internalSplit;
  if (
    'abbc'[$SPLIT](/(b)*/)[1] == 'c' ||
    'test'[$SPLIT](/(?:)/, -1)[LENGTH] != 4 ||
    'ab'[$SPLIT](/(?:ab)*/)[LENGTH] != 2 ||
    '.'[$SPLIT](/(.?)(.?)/)[LENGTH] != 4 ||
    '.'[$SPLIT](/()()/)[LENGTH] > 1 ||
    ''[$SPLIT](/.?/)[LENGTH]
  ) {
    // based on es5-shim implementation, need to rework it
    internalSplit = function (separator, limit) {
      var string = String(this);
      if (separator === undefined && limit === 0) return [];
      // If `separator` is not a regex, use native split
      if (!_isRegexp(separator)) return $split.call(string, separator, limit);
      var output = [];
      var flags = (separator.ignoreCase ? 'i' : '') +
                  (separator.multiline ? 'm' : '') +
                  (separator.unicode ? 'u' : '') +
                  (separator.sticky ? 'y' : '');
      var lastLastIndex = 0;
      var splitLimit = limit === undefined ? MAX_UINT32 : limit >>> 0;
      // Make `global` and avoid `lastIndex` issues by working with a copy
      var separatorCopy = new RegExp(separator.source, flags + 'g');
      var match, lastIndex, lastLength;
      while (match = _regexpExec.call(separatorCopy, string)) {
        lastIndex = separatorCopy[LAST_INDEX$1];
        if (lastIndex > lastLastIndex) {
          output.push(string.slice(lastLastIndex, match.index));
          if (match[LENGTH] > 1 && match.index < string[LENGTH]) $push.apply(output, match.slice(1));
          lastLength = match[0][LENGTH];
          lastLastIndex = lastIndex;
          if (output[LENGTH] >= splitLimit) break;
        }
        if (separatorCopy[LAST_INDEX$1] === match.index) separatorCopy[LAST_INDEX$1]++; // Avoid an infinite loop
      }
      if (lastLastIndex === string[LENGTH]) {
        if (lastLength || !separatorCopy.test('')) output.push('');
      } else output.push(string.slice(lastLastIndex));
      return output[LENGTH] > splitLimit ? output.slice(0, splitLimit) : output;
    };
  // Chakra, V8
  } else if ('0'[$SPLIT](undefined, 0)[LENGTH]) {
    internalSplit = function (separator, limit) {
      return separator === undefined && limit === 0 ? [] : $split.call(this, separator, limit);
    };
  } else {
    internalSplit = $split;
  }

  return [
    // `String.prototype.split` method
    // https://tc39.github.io/ecma262/#sec-string.prototype.split
    function split(separator, limit) {
      var O = defined(this);
      var splitter = separator == undefined ? undefined : separator[SPLIT];
      return splitter !== undefined
        ? splitter.call(separator, O, limit)
        : internalSplit.call(String(O), separator, limit);
    },
    // `RegExp.prototype[@@split]` method
    // https://tc39.github.io/ecma262/#sec-regexp.prototype-@@split
    //
    // NOTE: This cannot be properly polyfilled in engines that don't support
    // the 'y' flag.
    function (regexp, limit) {
      var res = maybeCallNative(internalSplit, regexp, this, limit, internalSplit !== $split);
      if (res.done) return res.value;

      var rx = _anObject(regexp);
      var S = String(this);
      var C = _speciesConstructor(rx, RegExp);

      var unicodeMatching = rx.unicode;
      var flags = (rx.ignoreCase ? 'i' : '') +
                  (rx.multiline ? 'm' : '') +
                  (rx.unicode ? 'u' : '') +
                  (SUPPORTS_Y ? 'y' : 'g');

      // ^(? + rx + ) is needed, in combination with some S slicing, to
      // simulate the 'y' flag.
      var splitter = new C(SUPPORTS_Y ? rx : '^(?:' + rx.source + ')', flags);
      var lim = limit === undefined ? MAX_UINT32 : limit >>> 0;
      if (lim === 0) return [];
      if (S.length === 0) return _regexpExecAbstract(splitter, S) === null ? [S] : [];
      var p = 0;
      var q = 0;
      var A = [];
      while (q < S.length) {
        splitter.lastIndex = SUPPORTS_Y ? q : 0;
        var z = _regexpExecAbstract(splitter, SUPPORTS_Y ? S : S.slice(q));
        var e;
        if (
          z === null ||
          (e = $min(_toLength(splitter.lastIndex + (SUPPORTS_Y ? 0 : q)), S.length)) === p
        ) {
          q = _advanceStringIndex(S, q, unicodeMatching);
        } else {
          A.push(S.slice(p, q));
          if (A.length === lim) return A;
          for (var i = 1; i <= z.length - 1; i++) {
            A.push(z[i]);
            if (A.length === lim) return A;
          }
          q = p = e;
        }
      }
      A.push(S.slice(p));
      return A;
    }
  ];
});

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

var _anInstance = function (it, Constructor, name, forbiddenField) {
  if (!(it instanceof Constructor) || (forbiddenField !== undefined && forbiddenField in it)) {
    throw TypeError(name + ': incorrect invocation!');
  } return it;
};

// call something on iterator step with safe closing on error

var _iterCall = function (iterator, fn, value, entries) {
  try {
    return entries ? fn(_anObject(value)[0], value[1]) : fn(value);
  // 7.4.6 IteratorClose(iterator, completion)
  } catch (e) {
    var ret = iterator['return'];
    if (ret !== undefined) _anObject(ret.call(iterator));
    throw e;
  }
};

var _iterators = {};

// check on default Array iterator

var ITERATOR = _wks('iterator');
var ArrayProto = Array.prototype;

var _isArrayIter = function (it) {
  return it !== undefined && (_iterators.Array === it || ArrayProto[ITERATOR] === it);
};

var ITERATOR$1 = _wks('iterator');

var core_getIteratorMethod = _core.getIteratorMethod = function (it) {
  if (it != undefined) return it[ITERATOR$1]
    || it['@@iterator']
    || _iterators[_classof(it)];
};

var _forOf = createCommonjsModule(function (module) {
var BREAK = {};
var RETURN = {};
var exports = module.exports = function (iterable, entries, fn, that, ITERATOR) {
  var iterFn = ITERATOR ? function () { return iterable; } : core_getIteratorMethod(iterable);
  var f = _ctx(fn, that, entries ? 2 : 1);
  var index = 0;
  var length, step, iterator, result;
  if (typeof iterFn != 'function') throw TypeError(iterable + ' is not iterable!');
  // fast case for arrays with default iterator
  if (_isArrayIter(iterFn)) for (length = _toLength(iterable.length); length > index; index++) {
    result = entries ? f(_anObject(step = iterable[index])[0], step[1]) : f(iterable[index]);
    if (result === BREAK || result === RETURN) return result;
  } else for (iterator = iterFn.call(iterable); !(step = iterator.next()).done;) {
    result = _iterCall(iterator, f, step.value, entries);
    if (result === BREAK || result === RETURN) return result;
  }
};
exports.BREAK = BREAK;
exports.RETURN = RETURN;
});

// fast apply, http://jsperf.lnkit.com/fast-apply/5
var _invoke = function (fn, args, that) {
  var un = that === undefined;
  switch (args.length) {
    case 0: return un ? fn()
                      : fn.call(that);
    case 1: return un ? fn(args[0])
                      : fn.call(that, args[0]);
    case 2: return un ? fn(args[0], args[1])
                      : fn.call(that, args[0], args[1]);
    case 3: return un ? fn(args[0], args[1], args[2])
                      : fn.call(that, args[0], args[1], args[2]);
    case 4: return un ? fn(args[0], args[1], args[2], args[3])
                      : fn.call(that, args[0], args[1], args[2], args[3]);
  } return fn.apply(that, args);
};

var document$2 = _global.document;
var _html = document$2 && document$2.documentElement;

var process = _global.process;
var setTask = _global.setImmediate;
var clearTask = _global.clearImmediate;
var MessageChannel = _global.MessageChannel;
var Dispatch = _global.Dispatch;
var counter = 0;
var queue = {};
var ONREADYSTATECHANGE = 'onreadystatechange';
var defer, channel, port;
var run = function () {
  var id = +this;
  // eslint-disable-next-line no-prototype-builtins
  if (queue.hasOwnProperty(id)) {
    var fn = queue[id];
    delete queue[id];
    fn();
  }
};
var listener = function (event) {
  run.call(event.data);
};
// Node.js 0.9+ & IE10+ has setImmediate, otherwise:
if (!setTask || !clearTask) {
  setTask = function setImmediate(fn) {
    var args = [];
    var i = 1;
    while (arguments.length > i) args.push(arguments[i++]);
    queue[++counter] = function () {
      // eslint-disable-next-line no-new-func
      _invoke(typeof fn == 'function' ? fn : Function(fn), args);
    };
    defer(counter);
    return counter;
  };
  clearTask = function clearImmediate(id) {
    delete queue[id];
  };
  // Node.js 0.8-
  if (_cof(process) == 'process') {
    defer = function (id) {
      process.nextTick(_ctx(run, id, 1));
    };
  // Sphere (JS game engine) Dispatch API
  } else if (Dispatch && Dispatch.now) {
    defer = function (id) {
      Dispatch.now(_ctx(run, id, 1));
    };
  // Browsers with MessageChannel, includes WebWorkers
  } else if (MessageChannel) {
    channel = new MessageChannel();
    port = channel.port2;
    channel.port1.onmessage = listener;
    defer = _ctx(port.postMessage, port, 1);
  // Browsers with postMessage, skip WebWorkers
  // IE8 has postMessage, but it's sync & typeof its postMessage is 'object'
  } else if (_global.addEventListener && typeof postMessage == 'function' && !_global.importScripts) {
    defer = function (id) {
      _global.postMessage(id + '', '*');
    };
    _global.addEventListener('message', listener, false);
  // IE8-
  } else if (ONREADYSTATECHANGE in _domCreate('script')) {
    defer = function (id) {
      _html.appendChild(_domCreate('script'))[ONREADYSTATECHANGE] = function () {
        _html.removeChild(this);
        run.call(id);
      };
    };
  // Rest old browsers
  } else {
    defer = function (id) {
      setTimeout(_ctx(run, id, 1), 0);
    };
  }
}
var _task = {
  set: setTask,
  clear: clearTask
};

var macrotask = _task.set;
var Observer = _global.MutationObserver || _global.WebKitMutationObserver;
var process$1 = _global.process;
var Promise$1 = _global.Promise;
var isNode = _cof(process$1) == 'process';

var _microtask = function () {
  var head, last, notify;

  var flush = function () {
    var parent, fn;
    if (isNode && (parent = process$1.domain)) parent.exit();
    while (head) {
      fn = head.fn;
      head = head.next;
      try {
        fn();
      } catch (e) {
        if (head) notify();
        else last = undefined;
        throw e;
      }
    } last = undefined;
    if (parent) parent.enter();
  };

  // Node.js
  if (isNode) {
    notify = function () {
      process$1.nextTick(flush);
    };
  // browsers with MutationObserver, except iOS Safari - https://github.com/zloirock/core-js/issues/339
  } else if (Observer && !(_global.navigator && _global.navigator.standalone)) {
    var toggle = true;
    var node = document.createTextNode('');
    new Observer(flush).observe(node, { characterData: true }); // eslint-disable-line no-new
    notify = function () {
      node.data = toggle = !toggle;
    };
  // environments with maybe non-completely correct, but existent Promise
  } else if (Promise$1 && Promise$1.resolve) {
    // Promise.resolve without an argument throws an error in LG WebOS 2
    var promise = Promise$1.resolve(undefined);
    notify = function () {
      promise.then(flush);
    };
  // for other environments - macrotask based on:
  // - setImmediate
  // - MessageChannel
  // - window.postMessag
  // - onreadystatechange
  // - setTimeout
  } else {
    notify = function () {
      // strange IE + webpack dev server bug - use .call(global)
      macrotask.call(_global, flush);
    };
  }

  return function (fn) {
    var task = { fn: fn, next: undefined };
    if (last) last.next = task;
    if (!head) {
      head = task;
      notify();
    } last = task;
  };
};

// 25.4.1.5 NewPromiseCapability(C)


function PromiseCapability(C) {
  var resolve, reject;
  this.promise = new C(function ($$resolve, $$reject) {
    if (resolve !== undefined || reject !== undefined) throw TypeError('Bad Promise constructor');
    resolve = $$resolve;
    reject = $$reject;
  });
  this.resolve = _aFunction(resolve);
  this.reject = _aFunction(reject);
}

var f$3 = function (C) {
  return new PromiseCapability(C);
};

var _newPromiseCapability = {
	f: f$3
};

var _perform = function (exec) {
  try {
    return { e: false, v: exec() };
  } catch (e) {
    return { e: true, v: e };
  }
};

var navigator = _global.navigator;

var _userAgent = navigator && navigator.userAgent || '';

var _promiseResolve = function (C, x) {
  _anObject(C);
  if (_isObject(x) && x.constructor === C) return x;
  var promiseCapability = _newPromiseCapability.f(C);
  var resolve = promiseCapability.resolve;
  resolve(x);
  return promiseCapability.promise;
};

var _redefineAll = function (target, src, safe) {
  for (var key in src) _redefine(target, key, src[key], safe);
  return target;
};

var def = _objectDp.f;

var TAG$1 = _wks('toStringTag');

var _setToStringTag = function (it, tag, stat) {
  if (it && !_has(it = stat ? it : it.prototype, TAG$1)) def(it, TAG$1, { configurable: true, value: tag });
};

var SPECIES$2 = _wks('species');

var _setSpecies = function (KEY) {
  var C = _global[KEY];
  if (_descriptors && C && !C[SPECIES$2]) _objectDp.f(C, SPECIES$2, {
    configurable: true,
    get: function () { return this; }
  });
};

var ITERATOR$2 = _wks('iterator');
var SAFE_CLOSING = false;

try {
  var riter = [7][ITERATOR$2]();
  riter['return'] = function () { SAFE_CLOSING = true; };
} catch (e) { /* empty */ }

var _iterDetect = function (exec, skipClosing) {
  if (!skipClosing && !SAFE_CLOSING) return false;
  var safe = false;
  try {
    var arr = [7];
    var iter = arr[ITERATOR$2]();
    iter.next = function () { return { done: safe = true }; };
    arr[ITERATOR$2] = function () { return iter; };
    exec(arr);
  } catch (e) { /* empty */ }
  return safe;
};

var task = _task.set;
var microtask = _microtask();




var PROMISE = 'Promise';
var TypeError$1 = _global.TypeError;
var process$2 = _global.process;
var versions = process$2 && process$2.versions;
var v8 = versions && versions.v8 || '';
var $Promise = _global[PROMISE];
var isNode$1 = _classof(process$2) == 'process';
var empty = function () { /* empty */ };
var Internal, newGenericPromiseCapability, OwnPromiseCapability, Wrapper;
var newPromiseCapability = newGenericPromiseCapability = _newPromiseCapability.f;

var USE_NATIVE = !!function () {
  try {
    // correct subclassing with @@species support
    var promise = $Promise.resolve(1);
    var FakePromise = (promise.constructor = {})[_wks('species')] = function (exec) {
      exec(empty, empty);
    };
    // unhandled rejections tracking support, NodeJS Promise without it fails @@species test
    return (isNode$1 || typeof PromiseRejectionEvent == 'function')
      && promise.then(empty) instanceof FakePromise
      // v8 6.6 (Node 10 and Chrome 66) have a bug with resolving custom thenables
      // https://bugs.chromium.org/p/chromium/issues/detail?id=830565
      // we can't detect it synchronously, so just check versions
      && v8.indexOf('6.6') !== 0
      && _userAgent.indexOf('Chrome/66') === -1;
  } catch (e) { /* empty */ }
}();

// helpers
var isThenable = function (it) {
  var then;
  return _isObject(it) && typeof (then = it.then) == 'function' ? then : false;
};
var notify = function (promise, isReject) {
  if (promise._n) return;
  promise._n = true;
  var chain = promise._c;
  microtask(function () {
    var value = promise._v;
    var ok = promise._s == 1;
    var i = 0;
    var run = function (reaction) {
      var handler = ok ? reaction.ok : reaction.fail;
      var resolve = reaction.resolve;
      var reject = reaction.reject;
      var domain = reaction.domain;
      var result, then, exited;
      try {
        if (handler) {
          if (!ok) {
            if (promise._h == 2) onHandleUnhandled(promise);
            promise._h = 1;
          }
          if (handler === true) result = value;
          else {
            if (domain) domain.enter();
            result = handler(value); // may throw
            if (domain) {
              domain.exit();
              exited = true;
            }
          }
          if (result === reaction.promise) {
            reject(TypeError$1('Promise-chain cycle'));
          } else if (then = isThenable(result)) {
            then.call(result, resolve, reject);
          } else resolve(result);
        } else reject(value);
      } catch (e) {
        if (domain && !exited) domain.exit();
        reject(e);
      }
    };
    while (chain.length > i) run(chain[i++]); // variable length - can't use forEach
    promise._c = [];
    promise._n = false;
    if (isReject && !promise._h) onUnhandled(promise);
  });
};
var onUnhandled = function (promise) {
  task.call(_global, function () {
    var value = promise._v;
    var unhandled = isUnhandled(promise);
    var result, handler, console;
    if (unhandled) {
      result = _perform(function () {
        if (isNode$1) {
          process$2.emit('unhandledRejection', value, promise);
        } else if (handler = _global.onunhandledrejection) {
          handler({ promise: promise, reason: value });
        } else if ((console = _global.console) && console.error) {
          console.error('Unhandled promise rejection', value);
        }
      });
      // Browsers should not trigger `rejectionHandled` event if it was handled here, NodeJS - should
      promise._h = isNode$1 || isUnhandled(promise) ? 2 : 1;
    } promise._a = undefined;
    if (unhandled && result.e) throw result.v;
  });
};
var isUnhandled = function (promise) {
  return promise._h !== 1 && (promise._a || promise._c).length === 0;
};
var onHandleUnhandled = function (promise) {
  task.call(_global, function () {
    var handler;
    if (isNode$1) {
      process$2.emit('rejectionHandled', promise);
    } else if (handler = _global.onrejectionhandled) {
      handler({ promise: promise, reason: promise._v });
    }
  });
};
var $reject = function (value) {
  var promise = this;
  if (promise._d) return;
  promise._d = true;
  promise = promise._w || promise; // unwrap
  promise._v = value;
  promise._s = 2;
  if (!promise._a) promise._a = promise._c.slice();
  notify(promise, true);
};
var $resolve = function (value) {
  var promise = this;
  var then;
  if (promise._d) return;
  promise._d = true;
  promise = promise._w || promise; // unwrap
  try {
    if (promise === value) throw TypeError$1("Promise can't be resolved itself");
    if (then = isThenable(value)) {
      microtask(function () {
        var wrapper = { _w: promise, _d: false }; // wrap
        try {
          then.call(value, _ctx($resolve, wrapper, 1), _ctx($reject, wrapper, 1));
        } catch (e) {
          $reject.call(wrapper, e);
        }
      });
    } else {
      promise._v = value;
      promise._s = 1;
      notify(promise, false);
    }
  } catch (e) {
    $reject.call({ _w: promise, _d: false }, e); // wrap
  }
};

// constructor polyfill
if (!USE_NATIVE) {
  // 25.4.3.1 Promise(executor)
  $Promise = function Promise(executor) {
    _anInstance(this, $Promise, PROMISE, '_h');
    _aFunction(executor);
    Internal.call(this);
    try {
      executor(_ctx($resolve, this, 1), _ctx($reject, this, 1));
    } catch (err) {
      $reject.call(this, err);
    }
  };
  // eslint-disable-next-line no-unused-vars
  Internal = function Promise(executor) {
    this._c = [];             // <- awaiting reactions
    this._a = undefined;      // <- checked in isUnhandled reactions
    this._s = 0;              // <- state
    this._d = false;          // <- done
    this._v = undefined;      // <- value
    this._h = 0;              // <- rejection state, 0 - default, 1 - handled, 2 - unhandled
    this._n = false;          // <- notify
  };
  Internal.prototype = _redefineAll($Promise.prototype, {
    // 25.4.5.3 Promise.prototype.then(onFulfilled, onRejected)
    then: function then(onFulfilled, onRejected) {
      var reaction = newPromiseCapability(_speciesConstructor(this, $Promise));
      reaction.ok = typeof onFulfilled == 'function' ? onFulfilled : true;
      reaction.fail = typeof onRejected == 'function' && onRejected;
      reaction.domain = isNode$1 ? process$2.domain : undefined;
      this._c.push(reaction);
      if (this._a) this._a.push(reaction);
      if (this._s) notify(this, false);
      return reaction.promise;
    },
    // 25.4.5.1 Promise.prototype.catch(onRejected)
    'catch': function (onRejected) {
      return this.then(undefined, onRejected);
    }
  });
  OwnPromiseCapability = function () {
    var promise = new Internal();
    this.promise = promise;
    this.resolve = _ctx($resolve, promise, 1);
    this.reject = _ctx($reject, promise, 1);
  };
  _newPromiseCapability.f = newPromiseCapability = function (C) {
    return C === $Promise || C === Wrapper
      ? new OwnPromiseCapability(C)
      : newGenericPromiseCapability(C);
  };
}

_export(_export.G + _export.W + _export.F * !USE_NATIVE, { Promise: $Promise });
_setToStringTag($Promise, PROMISE);
_setSpecies(PROMISE);
Wrapper = _core[PROMISE];

// statics
_export(_export.S + _export.F * !USE_NATIVE, PROMISE, {
  // 25.4.4.5 Promise.reject(r)
  reject: function reject(r) {
    var capability = newPromiseCapability(this);
    var $$reject = capability.reject;
    $$reject(r);
    return capability.promise;
  }
});
_export(_export.S + _export.F * (!USE_NATIVE), PROMISE, {
  // 25.4.4.6 Promise.resolve(x)
  resolve: function resolve(x) {
    return _promiseResolve(this, x);
  }
});
_export(_export.S + _export.F * !(USE_NATIVE && _iterDetect(function (iter) {
  $Promise.all(iter)['catch'](empty);
})), PROMISE, {
  // 25.4.4.1 Promise.all(iterable)
  all: function all(iterable) {
    var C = this;
    var capability = newPromiseCapability(C);
    var resolve = capability.resolve;
    var reject = capability.reject;
    var result = _perform(function () {
      var values = [];
      var index = 0;
      var remaining = 1;
      _forOf(iterable, false, function (promise) {
        var $index = index++;
        var alreadyCalled = false;
        values.push(undefined);
        remaining++;
        C.resolve(promise).then(function (value) {
          if (alreadyCalled) return;
          alreadyCalled = true;
          values[$index] = value;
          --remaining || resolve(values);
        }, reject);
      });
      --remaining || resolve(values);
    });
    if (result.e) reject(result.v);
    return capability.promise;
  },
  // 25.4.4.4 Promise.race(iterable)
  race: function race(iterable) {
    var C = this;
    var capability = newPromiseCapability(C);
    var reject = capability.reject;
    var result = _perform(function () {
      _forOf(iterable, false, function (promise) {
        C.resolve(promise).then(capability.resolve, reject);
      });
    });
    if (result.e) reject(result.v);
    return capability.promise;
  }
});

// 22.1.3.31 Array.prototype[@@unscopables]
var UNSCOPABLES = _wks('unscopables');
var ArrayProto$1 = Array.prototype;
if (ArrayProto$1[UNSCOPABLES] == undefined) _hide(ArrayProto$1, UNSCOPABLES, {});
var _addToUnscopables = function (key) {
  ArrayProto$1[UNSCOPABLES][key] = true;
};

var _iterStep = function (done, value) {
  return { value: value, done: !!done };
};

var _objectDps = _descriptors ? Object.defineProperties : function defineProperties(O, Properties) {
  _anObject(O);
  var keys = _objectKeys(Properties);
  var length = keys.length;
  var i = 0;
  var P;
  while (length > i) _objectDp.f(O, P = keys[i++], Properties[P]);
  return O;
};

// 19.1.2.2 / 15.2.3.5 Object.create(O [, Properties])



var IE_PROTO$1 = _sharedKey('IE_PROTO');
var Empty = function () { /* empty */ };
var PROTOTYPE$1 = 'prototype';

// Create object with fake `null` prototype: use iframe Object with cleared prototype
var createDict = function () {
  // Thrash, waste and sodomy: IE GC bug
  var iframe = _domCreate('iframe');
  var i = _enumBugKeys.length;
  var lt = '<';
  var gt = '>';
  var iframeDocument;
  iframe.style.display = 'none';
  _html.appendChild(iframe);
  iframe.src = 'javascript:'; // eslint-disable-line no-script-url
  // createDict = iframe.contentWindow.Object;
  // html.removeChild(iframe);
  iframeDocument = iframe.contentWindow.document;
  iframeDocument.open();
  iframeDocument.write(lt + 'script' + gt + 'document.F=Object' + lt + '/script' + gt);
  iframeDocument.close();
  createDict = iframeDocument.F;
  while (i--) delete createDict[PROTOTYPE$1][_enumBugKeys[i]];
  return createDict();
};

var _objectCreate = Object.create || function create(O, Properties) {
  var result;
  if (O !== null) {
    Empty[PROTOTYPE$1] = _anObject(O);
    result = new Empty();
    Empty[PROTOTYPE$1] = null;
    // add "__proto__" for Object.getPrototypeOf polyfill
    result[IE_PROTO$1] = O;
  } else result = createDict();
  return Properties === undefined ? result : _objectDps(result, Properties);
};

var IteratorPrototype = {};

// 25.1.2.1.1 %IteratorPrototype%[@@iterator]()
_hide(IteratorPrototype, _wks('iterator'), function () { return this; });

var _iterCreate = function (Constructor, NAME, next) {
  Constructor.prototype = _objectCreate(IteratorPrototype, { next: _propertyDesc(1, next) });
  _setToStringTag(Constructor, NAME + ' Iterator');
};

// 19.1.2.9 / 15.2.3.2 Object.getPrototypeOf(O)


var IE_PROTO$2 = _sharedKey('IE_PROTO');
var ObjectProto = Object.prototype;

var _objectGpo = Object.getPrototypeOf || function (O) {
  O = _toObject(O);
  if (_has(O, IE_PROTO$2)) return O[IE_PROTO$2];
  if (typeof O.constructor == 'function' && O instanceof O.constructor) {
    return O.constructor.prototype;
  } return O instanceof Object ? ObjectProto : null;
};

var ITERATOR$3 = _wks('iterator');
var BUGGY = !([].keys && 'next' in [].keys()); // Safari has buggy iterators w/o `next`
var FF_ITERATOR = '@@iterator';
var KEYS = 'keys';
var VALUES = 'values';

var returnThis = function () { return this; };

var _iterDefine = function (Base, NAME, Constructor, next, DEFAULT, IS_SET, FORCED) {
  _iterCreate(Constructor, NAME, next);
  var getMethod = function (kind) {
    if (!BUGGY && kind in proto) return proto[kind];
    switch (kind) {
      case KEYS: return function keys() { return new Constructor(this, kind); };
      case VALUES: return function values() { return new Constructor(this, kind); };
    } return function entries() { return new Constructor(this, kind); };
  };
  var TAG = NAME + ' Iterator';
  var DEF_VALUES = DEFAULT == VALUES;
  var VALUES_BUG = false;
  var proto = Base.prototype;
  var $native = proto[ITERATOR$3] || proto[FF_ITERATOR] || DEFAULT && proto[DEFAULT];
  var $default = $native || getMethod(DEFAULT);
  var $entries = DEFAULT ? !DEF_VALUES ? $default : getMethod('entries') : undefined;
  var $anyNative = NAME == 'Array' ? proto.entries || $native : $native;
  var methods, key, IteratorPrototype;
  // Fix native
  if ($anyNative) {
    IteratorPrototype = _objectGpo($anyNative.call(new Base()));
    if (IteratorPrototype !== Object.prototype && IteratorPrototype.next) {
      // Set @@toStringTag to native iterators
      _setToStringTag(IteratorPrototype, TAG, true);
      // fix for some old engines
      if (typeof IteratorPrototype[ITERATOR$3] != 'function') _hide(IteratorPrototype, ITERATOR$3, returnThis);
    }
  }
  // fix Array#{values, @@iterator}.name in V8 / FF
  if (DEF_VALUES && $native && $native.name !== VALUES) {
    VALUES_BUG = true;
    $default = function values() { return $native.call(this); };
  }
  // Define iterator
  if (BUGGY || VALUES_BUG || !proto[ITERATOR$3]) {
    _hide(proto, ITERATOR$3, $default);
  }
  // Plug for library
  _iterators[NAME] = $default;
  _iterators[TAG] = returnThis;
  if (DEFAULT) {
    methods = {
      values: DEF_VALUES ? $default : getMethod(VALUES),
      keys: IS_SET ? $default : getMethod(KEYS),
      entries: $entries
    };
    if (FORCED) for (key in methods) {
      if (!(key in proto)) _redefine(proto, key, methods[key]);
    } else _export(_export.P + _export.F * (BUGGY || VALUES_BUG), NAME, methods);
  }
  return methods;
};

// 22.1.3.4 Array.prototype.entries()
// 22.1.3.13 Array.prototype.keys()
// 22.1.3.29 Array.prototype.values()
// 22.1.3.30 Array.prototype[@@iterator]()
var es6_array_iterator = _iterDefine(Array, 'Array', function (iterated, kind) {
  this._t = _toIobject(iterated); // target
  this._i = 0;                   // next index
  this._k = kind;                // kind
// 22.1.5.2.1 %ArrayIteratorPrototype%.next()
}, function () {
  var O = this._t;
  var kind = this._k;
  var index = this._i++;
  if (!O || index >= O.length) {
    this._t = undefined;
    return _iterStep(1);
  }
  if (kind == 'keys') return _iterStep(0, index);
  if (kind == 'values') return _iterStep(0, O[index]);
  return _iterStep(0, [index, O[index]]);
}, 'values');

// argumentsList[@@iterator] is %ArrayProto_values% (9.4.4.6, 9.4.4.7)
_iterators.Arguments = _iterators.Array;

_addToUnscopables('keys');
_addToUnscopables('values');
_addToUnscopables('entries');

var ITERATOR$4 = _wks('iterator');
var TO_STRING_TAG = _wks('toStringTag');
var ArrayValues = _iterators.Array;

var DOMIterables = {
  CSSRuleList: true, // TODO: Not spec compliant, should be false.
  CSSStyleDeclaration: false,
  CSSValueList: false,
  ClientRectList: false,
  DOMRectList: false,
  DOMStringList: false,
  DOMTokenList: true,
  DataTransferItemList: false,
  FileList: false,
  HTMLAllCollection: false,
  HTMLCollection: false,
  HTMLFormElement: false,
  HTMLSelectElement: false,
  MediaList: true, // TODO: Not spec compliant, should be false.
  MimeTypeArray: false,
  NamedNodeMap: false,
  NodeList: true,
  PaintRequestList: false,
  Plugin: false,
  PluginArray: false,
  SVGLengthList: false,
  SVGNumberList: false,
  SVGPathSegList: false,
  SVGPointList: false,
  SVGStringList: false,
  SVGTransformList: false,
  SourceBufferList: false,
  StyleSheetList: true, // TODO: Not spec compliant, should be false.
  TextTrackCueList: false,
  TextTrackList: false,
  TouchList: false
};

for (var collections = _objectKeys(DOMIterables), i = 0; i < collections.length; i++) {
  var NAME = collections[i];
  var explicit = DOMIterables[NAME];
  var Collection = _global[NAME];
  var proto = Collection && Collection.prototype;
  var key;
  if (proto) {
    if (!proto[ITERATOR$4]) _hide(proto, ITERATOR$4, ArrayValues);
    if (!proto[TO_STRING_TAG]) _hide(proto, TO_STRING_TAG, NAME);
    _iterators[NAME] = ArrayValues;
    if (explicit) for (key in es6_array_iterator) if (!proto[key]) _redefine(proto, key, es6_array_iterator[key], true);
  }
}

// 19.1.3.6 Object.prototype.toString()

var test = {};
test[_wks('toStringTag')] = 'z';
if (test + '' != '[object z]') {
  _redefine(Object.prototype, 'toString', function toString() {
    return '[object ' + _classof(this) + ']';
  }, true);
}

// most Object methods by ES6 should accept primitives



var _objectSap = function (KEY, exec) {
  var fn = (_core.Object || {})[KEY] || Object[KEY];
  var exp = {};
  exp[KEY] = exec(fn);
  _export(_export.S + _export.F * _fails(function () { fn(1); }), 'Object', exp);
};

// 19.1.2.14 Object.keys(O)



_objectSap('keys', function () {
  return function keys(it) {
    return _objectKeys(_toObject(it));
  };
});

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

export { dawaAutocomplete };
