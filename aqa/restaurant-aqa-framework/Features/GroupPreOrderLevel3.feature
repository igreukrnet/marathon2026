Feature: Guest removal from group pre-order

  The owner can remove a guest from the group pre-order before submitting,
  recalculating the totals and ensuring the restaurant only receives the
  desired order items.

  Background:
    Given the restaurant has a waiter account
    And the manager has set up a new location
    And the manager adds a main course dish to the location

  Rule: The owner can remove a guest from the group

    @level3 @ui @smoke
    Scenario: The owner removes a guest on the storefront and the total updates
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And a guest called "Maria" joins the group and adds a dish
      When the owner opens the group pre-order page
      And the owner removes the guest "Maria" on the storefront
      Then the owner sees only participant "Alex"
      And the grand total equals the order of "Alex"

    @level3 @api
    Scenario: Removing a guest who already submitted their cart
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And the guest "Alex" submits their cart
      And a guest called "Maria" joins the group and adds a dish
      And the guest "Maria" submits their cart
      When the owner removes the guest "Maria" from the group
      Then the group participants contain only "Alex"

    @level3 @api
    Scenario: Removing the last guest leaves an empty group
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      When the owner removes the guest "Alex" from the group
      Then the group has no participants and a total of 0

  Rule: Guest removal enforces security and boundaries

    @level3 @api @negative
    Scenario: Removing a guest that belongs to another group is rejected
      Given the customer creates a group reservation called "GroupA"
      And a guest called "GuestA" joins the "GroupA" group
      And the customer creates a group reservation called "GroupB"
      When the owner of "GroupB" attempts to remove "GuestA" from "GroupB"
      Then removing the guest is rejected as not belonging to the group

    @level3 @api @negative
    Scenario: Removing a non-existent guest is rejected
      Given the customer creates a group reservation
      When the owner attempts to remove a non-existent guest
      Then the guest session is reported as not found

    @level3 @api @negative
    Scenario: A customer who is not the owner cannot remove a guest
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      When another customer attempts to remove the guest "Alex"
      Then removing the guest is forbidden

  Rule: End-to-end guest removal reflected in restaurant order

    @level3 @ui
    Scenario: Guests order dishes, owner removes one guest and waiter receives order without that guest
      Given the customer creates a group reservation
      And a guest called "Alex" joins the group and adds a dish
      And the guest "Alex" submits their cart
      And a guest called "Maria" joins the group and adds a dish
      And the guest "Maria" submits their cart
      When the owner opens the group pre-order page
      And the owner removes the guest "Maria" on the storefront
      And the owner submits the consolidated pre-order
      Then the waiter sees the reservation order containing only the dishes of "Alex"
