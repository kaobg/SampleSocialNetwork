$(function() {
    function Message(data, isOwn) {
        var self = this;
        self.Content = data.Content;
        self.TimeStamp = data.TimeStamp;
        self.IsOwn = isOwn;
    }

    function MessageGroup(isOwn) {
        var self = this;
        self.Messages = ko.observableArray();
        self.IsOwn = isOwn;
        self.TimeStamp = ko.observable();

        return self;
    }

    function MessageGroupByDay() {
        var self = this;
        self.MessageGroups = ko.observableArray();
        self.TimeStamp = ko.observable();
        self.LastMessageGroup;

        self.groupMessage = function(message) {
            if (self.LastMessageGroup == undefined) {
                self.LastMessageGroup = new MessageGroup(message.IsOwn);
                self.MessageGroups.push(self.LastMessageGroup);
            }

            if (self.LastMessageGroup.IsOwn != message.IsOwn) {
                self.LastMessageGroup = new MessageGroup(message.IsOwn);
                self.MessageGroups.push(self.LastMessageGroup);
            } 

            if (self.LastMessageGroup.TimeStamp() == undefined) {
                self.LastMessageGroup.TimeStamp(message.TimeStamp);
            }

            self.LastMessageGroup.Messages.push(message);
        }


        return self;
    }

    function ChatContext(data, hub) {
        var self = this;
        self.MessageGroupsByDays = ko.observableArray();
        self.LastMessageGroupByDay;
        self.NewMessages = ko.observableArray();
        self.NewMessage = ko.observable();
        self.LastInteraction = data.LastInteraction;
        self.Id = data.Id;
        self.OtherUser = data.OtherUser;
        self.MessageHistoryInitialized = false;
        self.IsLoadingMessageHistory = ko.observable(false);
        self.hub = hub;

        self.initializeHistory = function() {
            self.IsLoadingMessageHistory(true);
            self.hub.server.loadContextHistory(self.Id).done(function(data) {
                data = JSON.parse(data);
                var messages = $.map(data, function(item) {
                    var isOwn = item.SenderId != self.OtherUser.UserId;
                    return new Message(item, isOwn);
                });

                self.IsLoadingMessageHistory(false);
                self.MessageHistoryInitialized = true;
                self.groupMessages(messages);
            });
        }

        self.updateMessages = function() {
            for (var i = 0, l = self.NewMessages().length; i < l; i+=1) {
                self.groupMessage(self.NewMessages()[i]);
            }
            self.NewMessages.removeAll();
        }

        self.groupMessages = function(messages) {
            for (var i = 0, l = messages.length; i < l; i+=1) {
                self.groupMessage(messages[i]);
            }
        }

        self.groupMessage = function(message) {
            if (self.LastMessageGroupByDay == undefined) {
                self.LastMessageGroupByDay = new MessageGroupByDay();
                self.MessageGroupsByDays.push(self.LastMessageGroupByDay);
            } 

            if (self.LastMessageGroupByDay.TimeStamp() == undefined) {
                self.LastMessageGroupByDay.TimeStamp(message.TimeStamp); // takes the timestamp of the first message
            }

            var lastGroupDate = new Date(self.LastMessageGroupByDay.TimeStamp());
            var newMessageDate = new Date(message.TimeStamp);
            if (calculateDateDifferenceInDays(lastGroupDate, newMessageDate) > 0) {
                self.LastMessageGroupByDay = new MessageGroupByDay();
                self.LastMessageGroupByDay.TimeStamp(message.TimeStamp); // takes the timestamp of the first message
                self.MessageGroupsByDays.push(self.LastMessageGroupByDay);
            }

            self.LastMessageGroupByDay.groupMessage(message);
        }

        self.sendMessage = function(a, b, c, d, message) {
            if (message) {
                self.NewMessage(message); // bug fix for empty messages when pressing enter
            }

            if (!self.NewMessage()) {
                return;
            }

            self.hub.server.sendMessage(self.Id, self.NewMessage()).done(function(data) {
                data = JSON.parse(data);
                self.groupMessage(new Message(data, true));
                self.NewMessage('');
            }).fail(function(err) {
                alert(err);
            })
        }

        self.scrollToBottom = function() {
            $("#messagesGroupsByDaysList").slimScroll({ height: '400px', scrollTo: '99999999px'});
        }

        return self;
    }

    function viewModel() {
        var self = this;
        self.ChatContexts = ko.observableArray();
        self.CurrentContext = ko.observable();
        self.Visible = ko.observable(false);
        self.SendOnEnter = ko.observable(true); 
        self.NotificationsCount = ko.computed(function() {
            var count = $.grep(self.ChatContexts(), function(context) {
                return context.NewMessages().length > 0;
            }).length;
            return count;
        })
        self.hub = $.connection.messagesHub;
        self.CurrentUser;
        self.LastDocTitle;

        self.init = function() {
            self.hub.server.initialize().done(function(data) {
                data = JSON.parse(data);
                self.CurrentUser = data.CurrentUser;
                var contexts = $.map(data.ChatContexts, function(context) {
                    return new ChatContext(context, self.hub);
                });

                self.ChatContexts(contexts);
                urlManager.run();
            }).fail(function(err) {
                alert(err);
            });
        }

        self.switchContext = function(otherUser) {

            if (self.Visible() == false) {
                self.Visible(true);
            }

            var context = $.grep(self.ChatContexts(), function(item) {
                return item.OtherUser.UserName === otherUser;
            })[0];

            if (context == null) {
                self.hub.server.createContext(otherUser).done(function(data) {
                    data = JSON.parse(data);
                    context = new ChatContext(data, self.hub);
                    self.ChatContexts.unshift(context);
                    self.CurrentContext(context);
                    document.title = context.OtherUser.DisplayName + " - Messaging";
                    return;
                });
            }

            if (context == undefined) {
                return;
            }

            if (!context.MessageHistoryInitialized) {
                context.initializeHistory();
            }

            self.CurrentContext(context);
            self.CurrentContext().updateMessages();
            var li = $("#context-" + context.Id);
            li.parent().find(".currentContext").removeClass("currentContext");
            li.addClass("currentContext");
            document.title = context.OtherUser.DisplayName + " - Messaging";
        }

        self.toggleVisibility = function() {
            if (self.Visible() == false) {
                self.LastDocTitle = document.title;
                if (self.CurrentContext()) {
                    document.title = self.CurrentContext().OtherUser.DisplayName + " - Messaging";
                    self.CurrentContext().updateMessages();
                } else {
                    document.title = "Messaging";
                }
                self.Visible(true);
            } else {
                window.location.hash = "";
                document.title = self.LastDocTitle;
                self.Visible(false);
            }
        }

        self.error = function() {
            alert('generic error');
        }

        self.hub.client.newContext = function(data) {
            var newContext = new ChatContext(data, self.hub);
            self.ChatContexts.unshift(newContext);
        }

        self.hub.client.newMessage = function(message) {
            var chatContext = $.grep(self.ChatContexts(), function(context) {
                return context.Id === message.ChatContextId;
            })[0];

            if (self.CurrentContext() && self.Visible() && self.CurrentContext().Id == message.ChatContextId) {
                chatContext.groupMessage(new Message(message, false));
            } else {
                chatContext.NewMessages.push(new Message(message, false));
            }
        }



        var urlManager = $.sammy(function() {

            this.get("#/Messaging", function() {
                self.toggleVisibility();
            });

            this.get("#/Messaging/:user", function() {
                if (self.Visible() == false) {
                    self.Visible(true);
                }

                self.switchContext(this.params['user']);
            });
        });

        return self;

    };

    var vm = new viewModel();
    ko.applyBindings(vm, document.getElementById("footer"));

    // subscribe to the onconnected and ondisconnected events
    // otherwise they will not be fired
    $.connection.messagesHub.client.connected = function () { };
    $.connection.messagesHub.client.disconnected = function () { };

    window.hubReady.done(function() {
        vm.init();
    });

    ko.bindingHandlers.timeago = {
        init: function(element, valueAccessor) {
            var value = valueAccessor();
            $(element).attr("title", value);
            $(element).timeago();
        }
    };

    ko.bindingHandlers.time = {
        init: function(element, valueAccessor) {
            var value = ko.utils.unwrapObservable(valueAccessor());
            if  (value) {
                var timeString = value.substr(value.indexOf('T') + 1, 5);
                $(element).html(timeString);
            }
        },
        update: function(element, valueAccessor) {
            var value = ko.utils.unwrapObservable(valueAccessor());
            if  (value) {
                var timeString = value.substr(value.indexOf('T') + 1, 5);
                $(element).html(timeString);
            }
        }
    }

    ko.bindingHandlers.date = {
        init: function(element, valueAccessor) {
            var value = ko.utils.unwrapObservable(valueAccessor());
            if  (value) {
                var dateString = value.substr(0, value.indexOf('T'));
                $(element).html(dateString);
            }
        },
        update: function(element, valueAccessor) {
            var value = ko.utils.unwrapObservable(valueAccessor());
            if  (value) {
                var date = new Date(value);
                var dateString = formatDate(date);
                $(element).html(dateString);
            }
        }
    }

    ko.bindingHandlers.enterkey = {
        init: function(element, valueAccessor) {
            var params = ko.utils.unwrapObservable(valueAccessor());
            var func = params.callback;
            var value = params.value();

            if (value) {
                $(element).on('keypress', function(e) {
                    var keyCode = e.which || e.keyCode;
                    if (keyCode !== 13) {
                        return;
                    }
                    var message = $(element).val();
                    func(null, null, null, null, message);
                });
            }
        },
        
        update: function(element, valueAccessor) {
            var params = ko.utils.unwrapObservable(valueAccessor());
            var func = params.callback;
            var value = params.value();

            $(element).unbind('keypress');

            if (value) {
                $(element).on('keypress', function(e) {
                    var keyCode = e.which || e.keyCode;
                    if (keyCode !== 13) {
                        return;
                    }
                    var message = $(element).val();
                    if (value) {
                        func(null, null, null, null, message);
                    }
                });
            }
        },
    }

    ko.bindingHandlers.fadeVisible = {
        init: function(element, valueAccessor) {
            // Initially set the element to be instantly visible/hidden depending on the value
            var value = valueAccessor();
            $(element).toggle(ko.utils.unwrapObservable(value)); // Use "unwrapObservable" so we can handle values that may or may not be observable
        },
        update: function(element, valueAccessor) {
            // Whenever the value subsequently changes, slowly fade the element in or out
            var value = valueAccessor();
            ko.utils.unwrapObservable(value) ? $(element).slideDown('fast') : $(element).slideUp('fast');
        }
    };

    function formatDate(date) {
        var monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", 
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        var monthName = monthNames[date.getMonth()];
        var now = new Date();
        var dateString = monthName + " " + date.getDate();
        if (now.getFullYear() - date.getFullYear() != 0) {
            dateString += ", " + date.getFullYear();
        }
        return dateString;
    }

    function calculateDateDifferenceInDays(firstDate, secondDate) {
        var date1 = Date.UTC(firstDate.getFullYear(), firstDate.getMonth(), firstDate.getDate());
        var date2 = Date.UTC(secondDate.getFullYear(), secondDate.getMonth(), secondDate.getDate());
        var ms = Math.abs(date1-date2);
        return Math.floor(ms/1000/60/60/24); //floor should be unnecessary, but just in case
    }
});