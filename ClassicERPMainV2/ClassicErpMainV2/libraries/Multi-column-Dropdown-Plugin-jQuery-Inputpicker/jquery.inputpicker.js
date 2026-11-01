/**
 * jQuery-inputpicker - A jQuery input picker plugin. It supports showing multiple columns select in input.
 * Copyright (c) 2017 Ukalpa - https://ukalpa.com/inputpicker
 * License: MIT
 */

(function (factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
        define(['jquery'], factory);
    } else if (typeof exports === 'object') {
        // Node/CommonJS
        factory(require('jquery'));
    } else {
        // Browser globals
        factory(jQuery);
    }
})(function ($) {

    var methods;
    methods = {
        init: function (options) {
            return this.each(function () {
                var uuid = _generateUid();
                var original = $(this);
                var input;   // Shadow input

                if (original.hasClass('inputpicker-original') && _getInputpickerDiv(_i(original))) {
                    var orginial_css = original.data('inputpicker-original-css');
                    _getInputpickerDiv(_i(original)).closest('.inputpicker-overflow-hidden').remove();
                    original.removeClass(function (index, className) {
                        return (className.match(/\binputpicker-\S+/g) || []).join(' ');
                    });
                    original.css({
                        top: orginial_css['top'],
                        position: orginial_css['position']
                    });
                }
                {
                    original.data('inputpicker-uuid', uuid);
                    var ow = original.outerWidth();

                    // Clone input
                    var input = original.clone();

                    // Initiate input
                    input.val('').data('inputpicker-uuid', uuid).addClass('inputpicker-input').prop('id', 'inputpicker-' + uuid).prop('name', 'inputpicker-' + uuid);

                    // Inputpicker div ( wrap fake input and arrow )
                    var inputpicker_div = $("<div id=\"inputpicker-div-" + uuid + "\" class=\"inputpicker-div\" data-uuid=\"" + uuid + "\" style=\"position:relative;overflow:auto;height:100%;\"><span class=\"inputpicker-arrow\" data-uuid=\"" + uuid + "\" onclick=\"$(this).parent().find('input').inputpicker('toggle');event.stopPropagation();\"><b></b></span></div>")
                        .append(input)
                        .on('click', function (e) {
                            $(this).find('input').focus();
                            e.stopPropagation();
                            e.preventDefault();
                        })
                        .attr('unselectable', 'on')
                        .css('user-select', 'none')
                        .on('selectstart', false);

                    // Set width, do not put into the top html in terms of using responsive later.
                    inputpicker_div.css('width', ow + 'px');

                    var div_overflow_hidden = $("<div class=\"inputpicker-overflow-hidden\" style='overflow: hidden;'></div>");
                    original.after(div_overflow_hidden.append(inputpicker_div));

                    // Add Classes to the original element
                    original
                        .addClass('inputpicker-original')
                        .addClass('inputpicker-original-' + uuid)
                        .attr('tabindex', -1)
                        .data('inputpicker-input', input)
                        .data('inputpicker-original-css', {
                            'top': original.css('top'),
                            'position': original.css('position')
                        })
                        .css({
                            'position': 'fixed',
                            'top': '-1000px'
                        });

                    input.data('inputpicker-original', original)
                        .prop('autocomplete', 'off');

                    $.fn.inputpicker.elements.push(input);

                    // Start events handlers
                    original.on('focus.inputpicker', function () {
                        var original = $(this);
                        var input = _i(original);
                        input.trigger('focus');

                    }).on('change.inputpicker', function () {
                        var original = $(this);
                        var input = _i(original);
                        _setValue(input, original.val());
                    });

                    // input Events
                    input.on('focus.inputpicker', _eventFocus)
                        .on('blur.inputpicker', _eventBlur)
                        .on('keydown.inputpicker', _eventKeyDown)
                        .on('keyup.inputpicker', _eventKeyUp);
                    // End events handlers
                }

                // Start loading Settings
                var _options = [];
                for (var k in $.fn.inputpicker.defaults) {
                    if (input.data(k)) {
                        _options[k] = input.data(k);
                    }
                }

                // Merge settings by orders: 1.options > 2 attr > 3.defaults
                var settings = $.extend({ cache: {} }, $.fn.inputpicker.defaults, _options, Array.isArray(options) ? { data: options } : options);

                // Set default value for fieldText
                if (!settings['fieldText']) settings['fieldText'] = settings['fieldValue'];

                // Set default value for fields
                if (!settings['fields'].length) {
                    settings['fields'].push(settings['fieldText']);
                }

                _set(input, settings);

                // -------- Load & Show ------------------------------------
                _init(input);

                // Load data and set value
                _loadData(input, settings['data'], function (input) {
                    if (!_setValue(input, original.val())) {
                    }

                    // Open dropdown after data is loaded if openAfterLoad is set
                    if (_set(input, 'openAfterLoad')) {
                        var loadedData = _set(input, 'data');
                        var nextPickerSelector = _set(input, 'nextPicker');
                        var keyword = input.val().trim();

                        // Only jump to nextPicker if this is the initial load (no keyword typed)
                        if ((!loadedData || !loadedData.length) && nextPickerSelector && keyword.length === 0) {
                            _hideWrappedList();
                            var nextEl = $(nextPickerSelector);
                            if (nextEl.length) {
                                var isInputpicker = nextEl.hasClass('inputpicker-original') || nextEl.data('inputpicker-input');

                                if (isInputpicker) {
                                    nextEl.inputpicker('loadData', null, function (nextInput, nextData) {
                                        nextEl.inputpicker('show');
                                        nextEl.focus().select();
                                    });
                                } else {
                                    nextEl.focus().select();
                                }
                            }
                        } else {
                            methods.show.call(input);
                            input.focus().select();
                        }
                    }

                });
                _initPP();
            })
        },

        loadData: function (data, func) {
            return this.each(function () {
                var input = _i($(this));
                _loadData.call(input,
                    input, data, function (input, data) {
                        if (_setValue(input, _o(input).val())) {
                            _o(input).trigger('change');
                        }
                        if (typeof func == 'function') {
                            func.call(input, input, data);
                        }
                    });
            });
        },

        destroy: function (options) {
            return this.each(function () {
                var original = _o($(this));
                var input = _i(original);
                var wrapped_list = _getWrappedList(input);
                var uuid = _uuid(input);
                input.removeClass('inputpicker-input');
                input.removeData('inputpicker-settings');
                input.remove();
                wrapped_list.remove();

                var original_css = original.data('inputpicker-original-css');
                original.css('top', original_css['top']).css('position', original_css['position']);
                original.removeData('inputpicker-original-css');
                original.prop('tabindex', null);
                original.removeClass('inputpicker-original-' + uuid);
                original.off('focus.inputpicker');
                original.off('change.inputpicker');
            })
        },

        set: function (k, v) {
            var input = _i($(this));
            if (typeof k == 'undefined' && typeof v == 'undefined') {
                return _set(input);
            }
            if (typeof v == 'undefined') {
                return _set(input, k);
            }
            else {
                return _set(input, k, v);
            }
        },

        data: function (data) {
            if (typeof data == 'undefined') {
                return _set(_i($(this)), 'data');
            }
            else {
                return this.each(function () {
                    var input = _i($(this));
                    _set(input, 'data', _formatData(_set(input, 'fieldValue'), data));
                });
            }
        },

        data_highlighted: function () {
            var input = _i($(this));
            var fieldValue = _set(input, 'fieldValue');
            var data = _formatData(fieldValue, methods.data.call(input));
            var tr_active = _getWrappedListElements(true);
            if (tr_active.length) {
                var i = tr_active.data('i');
                return data[i];
            }
        },

        value_highlighted: function () {
            var input = _i($(this));
            var fieldValue = _set(input, 'fieldValue');
            var data = _formatData(fieldValue, methods.data.call(input));
            var tr_active = _getWrappedListElements(true);
            if (tr_active.length) {
                var i = tr_active.data('i');
                if (data[i]) {
                    return data[i][fieldValue];
                }
            }
        },

        element: function (value, field) {
            var original = _o($(this));
            var input = _i(original);
            if (typeof value == 'undefined') {
                value = original.val();
            }
            var fieldValue = (typeof field === 'undefined') ? _set(input, 'fieldValue') : field;

            var data = _set(input, 'data');
            if (!data.length) return null;
            for (var i = 0; i < data.length; i++) {
                if (data[i][fieldValue] == value) {
                    return data[i];
                }
            }
            return null;
        },

        toggle: function (e) {
            return this.each(function () {
                var input = _i($(this));
                if (_isWrappedListVisible(input)) {
                    methods.hide.call(input, e);
                }
                else {
                    if (input.is(":focus")) {
                        methods.show.call(input, e);
                    }
                    else {
                        input.focus();
                        if (!_set(input, 'autoOpen')) {
                            methods.show.call(input, e);
                        }
                    }
                }
            });
        },

        show: function (e) {
            return this.each(function () {
                var input = _i($(this));
                var uuid = _uuid(input);
                if (!_isInputVisible(input)) {
                    _alert('input[name=' + _name(input) + '] is not visible.');
                    return;
                }
                else if (_uuid(_getWrappedList()) == uuid) {
                    _getWrappedList().show();
                }
                else {
                    _getWrappedList(input).show();
                    _dataRender(input);
                }
            });
        },

        hide: function (e) {
            _hideWrappedList();
        },

        val: function (value) {
            return this.each(function () {
                var original = _o($(this));
                var input = _i(original);
                if (_setValue(input, value)) {
                    original.trigger('change');
                }
            });
        },

        is: function () {
            var input = _i($(this));
            dd(input);
        },

        removeValue: function (v) {
            return this.each(function () {
                var original = _o($(this));
                var input = _i(original);
                var value = _o(input).val();
                var delimiter = _set(input, 'delimiter');
                var arr_value = value ? value.toString().split(delimiter) : [];
                var i = _inArray(v, arr_value);

                if (i > -1) {
                    arr_value.splice(i, 1);
                    _setValue(input, arr_value.join(delimiter));
                    _hideWrappedList(true);
                }
            });
        },

        jumpToPage: function (page) {
            return this.each(function () {
                var original = _o($(this));
                var input = _i(original);

                _set(input, 'pageCurrent', page ? parseInt(page) : 1);
                _loadData.call(input,
                    input, null, function (input, data) {
                        _dataRender(input);
                    });
            });
        },

        debug: false
    };

    // Helper functions
    function dd() {
        if (methods.debug) {
            var args = Array.prototype.slice.call(arguments);
            console.log(args.length == 1 ? args[0] : args);
        }
    }

    function _name(input) {
        return _o(input).attr('name');
    }

    function _error(msg, input) {
        if (typeof input != 'undefined') {
            var original = _o(input);
            if (original) {
                if (original.attr('name')) {
                    msg += " for input[name=" + original.attr('name') + "]";
                }
                else if (original.attr('id')) {
                    msg += " for input[id=" + original.attr('id') + "]";
                }
            }
        }
        throw msg + " in inputpicker.js";
    }

    function _alert(msg, input) {
        if (typeof input != 'undefined') {
            if (_name(input)) {
                msg += ' input[name=' + _name(input) + ']';
            }
        }
        alert(msg);
    }

    function _formatData(fieldValue, data) {
        if (!Array.isArray(data)) {
            return [];
        }
        if (data.length && typeof data[0] != 'object') {
            var new_data = [];
            for (var i in data) {
                var o = {};
                o[fieldValue] = data[i];
                new_data.push(o);
            }
            data = new_data;
            new_data = null;
        }
        return data;
    }

    function _execJSON(input, param, func) {
        var url = _set(input, 'url');

        if (typeof param == 'undefined' && typeof func == 'undefined') {
            _alert('The param is incorrect. input[name=' + _name(input) + ']');
            return;
        }
        if (typeof func == 'undefined' && typeof param == 'function') {
            func = param;
            param = {};
        }
        if (typeof func != 'function') {
            _alert('The callback function is incorrect. input[name=' + _name(input) + ']');
            return;
        }

        if (_pagination(input)) {
            param[_set(input, 'pageField')] = _set(input, 'pageCurrent');
            param[_set(input, 'pageLimitField')] = _set(input, 'limit') ? _set(input, 'limit') : 10;
        }

        param = $.extend({
            q: input.val(),
            limit: _set(input, 'limit'),
            fieldValue: _set(input, 'fieldValue'),
            fieldText: _set(input, 'fieldText'),
            value: _o(input).val()
        }, _set(input, 'urlParam'), param);

        var param_serialised = 'urlParams|' + $.param(param);
        var cacheData = _cache(input, param_serialised);

        if (_set(input, 'urlCache')) {
            if (typeof cacheData == 'undefined') {
                $.get(url, param, function (ret) {
                    _cache(input, param_serialised, ret);
                    func(ret);
                }, "json");
            }
            else {
                func(cacheData);
            }
        }
        else {
            $.get(url, param, func, "json");
        }
    }

    function _init(input) {
        if (_typeIsMultiple(input)) {
            _initMultiple(input);
        }
        else if (_typeIsTag(input)) {
            _initTag(input);
        }

        if (_set(input, 'responsive')) {
            var inputpicker_div = _getInputpickerDiv(input);
            inputpicker_div.css('width', _set(input, 'width'));
            $(window).bind('resize', function () {
                _setWrappedListWidthAndHeight(input);
                _setWrappedListPosition(input);
            });
        }
    }

    function _initPP() {
        if ($.fn.popover) {
            $('[data-toggle="popover"]').popover();
        }
    }

    function _initMultiple(input) {
        var inputpicker_div = _getInputpickerDiv(input);
        if (!inputpicker_div.find('.inputpicker-multiple').length) {
            var inputpicker_multiple_div = $("<div class=\"inputpicker-multiple\" style=\"\"><ul class=\"\"></ul></div>").prependTo(inputpicker_div);

            inputpicker_div
                .attr('class', inputpicker_div.attr('class') + ' ' + input.attr('class'))
                .css('border', input.css('border'))
                .addClass('inputpicker-multiple')
                .css('left', '0px')
                .append("<span class=\"input-span\" style=\"display: none;\" ></span>");

            input.removeClass().width(1)
                .on('keyup.inputpicker.multiple', function (e) {
                    var input = $(this);
                    var inputpicker_div = _getInputpickerDiv(input);
                    var span = inputpicker_div.find('.input-span');

                    span.text(input.val());
                    input.width(span.width() + 15);
                    _setWrappedListPosition(input);
                });

            input.detach().appendTo($("<li class=\"inputpicker-multiple-input\"></li>").appendTo(inputpicker_multiple_div.find('ul')));
        }
    }

    function _initTag(input) {
        _initMultiple(input);
    }

    function _getInputpickerDiv(input) {
        return _i(input).closest('.inputpicker-div');
    }

    function _getInputMultipleElements(input) {
        var p = '.inputpicker-element';
        return _getInputpickerDiv(input).find(p);
    }

    function _getWrappedList(input) {
        var wrapped_list = $('#inputpicker-wrapped-list');
        if (!wrapped_list.length) {
            wrapped_list = $('<div />', {
                id: 'inputpicker-wrapped-list',
                tabindex: -1
            }).css({
                'display': 'none',
                'position': 'absolute',
                'overflow': 'auto'
            }).addClass('inputpicker-wrapped-list')
                .data('inputpicker-uuid', 0)
                .appendTo(document.body);

            if ($.fn.popover) {
                wrapped_list.popover();
            }

            $(document).on('click', function (e) {
                if (!($(e.target).hasClass('inputpicker-input'))) {
                    _hideWrappedList();
                }
            });

            wrapped_list.on('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
            });
        }

        if (typeof input != 'undefined') {
            if (_uuid(wrapped_list) != _uuid(input)) {
                _uuid(wrapped_list.html(""), _uuid(input));
            }
        }

        return wrapped_list;
    }

    function _getWrappedListElements(isActive) {
        var wrapped_list = _getWrappedList();
        return isActive ? wrapped_list.find('tr.active') : wrapped_list.find('tr');
    }

    function _hideWrappedList(focusOriginal) {
        var wrapped_list = _getWrappedList();
        if (wrapped_list.is(':visible')) {
            wrapped_list.hide();
        }
    }

    function _isWrappedListVisible(input) {
        var wrapped_list = _getWrappedList();
        return wrapped_list.is(':visible') && _uuid(wrapped_list) === _uuid(input);
    }

    function _isInputVisible(input) {
        return input.is(':visible');
    }

    function _uuid(el, value) {
        if (typeof value !== 'undefined') {
            return el.data('inputpicker-uuid', value);
        }
        return el.data('inputpicker-uuid');
    }

    function _generateUid() {
        return Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1);
    }

    function _i(original) {
        if (original.hasClass('inputpicker-original')) {
            return original.data('inputpicker-input');
        }
        return original;
    }

    function _o(input) {
        if (input.hasClass('inputpicker-input')) {
            return input.data('inputpicker-original');
        }
        return input;
    }

    function _set(input, k, v) {
        var settings = input.data('inputpicker-settings') || {};
        if (typeof k === 'undefined') return settings;
        if (typeof k === 'object') {
            settings = $.extend(settings, k);
            input.data('inputpicker-settings', settings);
            return settings;
        }
        if (typeof v === 'undefined') return settings[k];
        settings[k] = v;
        input.data('inputpicker-settings', settings);
        return v;
    }

    function _cache(input, k, v) {
        var cache = _set(input, 'cache') || {};
        if (typeof v === 'undefined') return cache[k];
        cache[k] = v;
        _set(input, 'cache', cache);
        return v;
    }

    function _loadData(input, data, func) {
        if (_set(input, 'url')) {
            _execJSON(input, {}, function (res) {
                var loadedData = Array.isArray(res) ? res : (res.data || []);
                _set(input, 'data', _formatData(_set(input, 'fieldValue'), loadedData));
                if (typeof func === 'function') func(input, _set(input, 'data'));
            });
        } else {
            if (typeof data !== 'undefined' && data !== null) {
                _set(input, 'data', _formatData(_set(input, 'fieldValue'), data));
            }
            if (typeof func === 'function') func(input, _set(input, 'data'));
        }
    }

    function _setValue(input, val) {
        var original = _o(input);
        var data = _set(input, 'data') || [];
        var fieldValue = _set(input, 'fieldValue');
        var fieldText = _set(input, 'fieldText');

        original.val(val);

        if (!data.length) {
            input.val(val);
            return true;
        }

        for (var i = 0; i < data.length; i++) {
            if (data[i][fieldValue] == val) {
                input.val(data[i][fieldText]);
                return true;
            }
        }
        input.val(val);
        return false;
    }

    function _pagination(input) {
        return _set(input, 'pagination');
    }

    function _typeIsMultiple(input) {
        return _set(input, 'multiple');
    }

    function _typeIsTag(input) {
        return _set(input, 'tag');
    }

    function _inArray(needle, haystack) {
        for (var i = 0; i < haystack.length; i++) {
            if (haystack[i] == needle) return i;
        }
        return -1;
    }

    function _setWrappedListWidthAndHeight(input) {
        var wrapped_list = _getWrappedList(input);
        var w = _set(input, 'width') || input.outerWidth();
        wrapped_list.css('width', w);
    }

    function _setWrappedListPosition(input) {
        var wrapped_list = _getWrappedList(input);
        var offset = input.offset();
        if (offset) {
            wrapped_list.css({
                top: offset.top + input.outerHeight(),
                left: offset.left
            });
        }
    }

    function _dataRender(input) {
        var wrapped_list = _getWrappedList(input);
        var data = _set(input, 'data') || [];
        var fields = _set(input, 'fields') || [];
        var fieldValue = _set(input, 'fieldValue');

        _setWrappedListWidthAndHeight(input);
        _setWrappedListPosition(input);

        var html = '<table class="table table-bordered table-striped inputpicker-table">';
        html += '<tbody>';
        for (var i = 0; i < data.length; i++) {
            html += '<tr data-i="' + i + '" data-value="' + data[i][fieldValue] + '">';
            for (var j = 0; j < fields.length; j++) {
                var f = typeof fields[j] === 'object' ? fields[j].name : fields[j];
                html += '<td>' + (data[i][f] !== undefined ? data[i][f] : '') + '</td>';
            }
            html += '</tr>';
        }
        html += '</tbody></table>';

        wrapped_list.html(html);

        wrapped_list.find('tr').on('click', function () {
            var val = $(this).data('value');
            _setValue(input, val);
            _o(input).trigger('change');
            _hideWrappedList();
        });
    }

    function _eventFocus(e) {
        var input = $(this);
        if (_set(input, 'autoOpen')) {
            methods.show.call(input, e);
        }
    }

    function _eventBlur(e) {
        // Handled via document click handler
    }

    function _eventKeyDown(e) {
        var input = $(this);
        if (e.keyCode === 13) { // Enter
            var active = _getWrappedListElements(true);
            if (active.length) {
                active.trigger('click');
            }
            e.preventDefault();
        }
    }

    function _eventKeyUp(e) {
        var input = $(this);
        if (e.keyCode !== 13 && e.keyCode !== 38 && e.keyCode !== 40) {
            if (_set(input, 'url')) {
                _execJSON(input, { q: input.val() }, function (res) {
                    var loadedData = Array.isArray(res) ? res : (res.data || []);
                    _set(input, 'data', _formatData(_set(input, 'fieldValue'), loadedData));
                    methods.show.call(input);
                });
            }
        }
    }

    $.fn.inputpicker = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        } else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        } else {
            $.error('Method ' + method + ' does not exist on jQuery.inputpicker');
        }
    };

    $.fn.inputpicker.elements = [];
    $.fn.inputpicker.defaults = {
        data: [],
        fields: [],
        fieldValue: 'value',
        fieldText: 'text',
        delimiter: ',',
        width: '100%',
        height: '200px',
        url: '',
        urlParam: {},
        urlCache: false,
        pagination: false,
        pageField: 'p',
        pageLimitField: 'per_page',
        pageCurrent: 1,
        limit: 10,
        autoOpen: false,
        openAfterLoad: false,
        nextPicker: '',
        multiple: false,
        tag: false,
        responsive: true
    };
});